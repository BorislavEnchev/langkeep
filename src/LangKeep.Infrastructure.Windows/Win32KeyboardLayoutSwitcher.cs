using System.Runtime.InteropServices;
using LangKeep.Core.Interfaces;
using LangKeep.Core.Models;
using LangKeep.Infrastructure.Windows.Interop;
using Microsoft.Extensions.Logging;

namespace LangKeep.Infrastructure.Windows;

/// <summary>
/// Switches the keyboard layout for the foreground window on Windows.
///
/// Strategy (tried in order):
/// <list type="number">
///   <item><c>SendInput</c> simulating Win+Space (atomic single call — most reliable across all apps).</item>
///   <item><c>PostMessage</c> with <c>WM_INPUTLANGCHANGEREQUEST</c> (backup for apps that ignore SendInput).</item>
/// </list>
///
/// After each attempt the current layout is verified with up to 3 retries (100 ms apart)
/// to allow slow apps time to process the layout change.
/// </summary>
public sealed class Win32KeyboardLayoutSwitcher : IKeyboardLayoutSwitcher
{
    private readonly ILogger<Win32KeyboardLayoutSwitcher> _logger;
    private readonly object _lock = new();

    private static readonly int InputSize = Marshal.SizeOf<Win32Native.INPUT>();

    public Win32KeyboardLayoutSwitcher(ILogger<Win32KeyboardLayoutSwitcher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool TrySwitchLayout(ApplicationIdentity application, KeyboardLayout targetLayout, IntPtr windowHandle = default)
    {
        // Lock to prevent concurrent switch attempts (SendInput especially must be serialized).
        lock (_lock)
        {
            _logger.LogDebug(
                "TrySwitchLayout start: {ProcessName} → {Layout} (HWND: 0x{Hwnd:X8})",
                application.ProcessName, targetLayout.LanguageTag, windowHandle.ToInt64());

            // Get the current layout before switching (for logging and to short-circuit).
            int beforeLangId = GetLayoutLangId(windowHandle);
            _logger.LogDebug(
                "Current layout before switch: LCID 0x{BeforeLangId:X4}",
                beforeLangId);

            int targetLangId = GetLayoutLangId(targetLayout);
            if (targetLangId == 0)
            {
                _logger.LogWarning(
                    "Unknown language tag '{Layout}' for {ProcessName}.",
                    targetLayout.LanguageTag, application.ProcessName);
                return false;
            }

            // ── Attempt 1: SendInput Win+Space (atomic) ──
            _logger.LogDebug(
                "Attempt 1 — SendInput Win+Space for {ProcessName} → {Layout}.",
                application.ProcessName, targetLayout.LanguageTag);

            if (TryViaSendInput(application, targetLayout, targetLangId, windowHandle))
                return true;

            // ── Attempt 2: PostMessage WM_INPUTLANGCHANGEREQUEST ──
            _logger.LogDebug(
                "Attempt 2 — PostMessage for {ProcessName} → {Layout}.",
                application.ProcessName, targetLayout.LanguageTag);

            if (TryViaPostMessage(application, targetLayout, targetLangId, windowHandle))
                return true;

            _logger.LogWarning(
                "Both SendInput and PostMessage failed for {ProcessName} → {Layout}.",
                application.ProcessName, targetLayout.LanguageTag);

            return false;
        }
    }

    // ───────────────────── SendInput Win+Space (atomic) ─────────────────────

    /// <summary>
    /// Simulates a single Win+Space press via <c>SendInput</c>.
    /// All 4 key events (Win down, Space down, Space up, Win up) are sent
    /// as a single atomic INPUT array so the OS recognises the hotkey.
    /// After pressing, waits and verifies the layout actually changed.
    /// If the target wasn't reached, retries with brute-force cycling.
    /// </summary>
    private bool TryViaSendInput(
        ApplicationIdentity application, KeyboardLayout targetLayout,
        int targetLangId, IntPtr windowHandle)
    {
        try
        {
            // First, ensure the target layout is loaded.
            string? klId = GetKeyboardLayoutId(targetLayout.LanguageTag);
            if (klId is null)
                return false;

            IntPtr targetHkl = Win32Native.LoadKeyboardLayout(klId, Win32Native.KLF_SUBSTITUTE_OK);
            if (targetHkl == IntPtr.Zero)
            {
                _logger.LogDebug(
                    "SendInput: LoadKeyboardLayout failed for {Layout}.",
                    targetLayout.LanguageTag);
                return false;
            }

            // Send a single Win+Space press via atomic SendInput.
            SendWinSpacePress();

            // Wait for the input to be processed and check.
            if (VerifyAfterAttempt(application, targetLayout, targetLangId, windowHandle, maxRetries: 5, retryDelayMs: 120))
                return true;

            // Didn't reach target with one press. The target might be more than
            // one Win+Space away if we lost track of the current layout.
            // Retrieve the actual loaded layouts and calculate/brute-force.
            _logger.LogDebug(
                "SendInput: one press didn't reach target {Layout}. Trying cycling.",
                targetLayout.LanguageTag);

            return CycleLayoutsUntilReached(application, targetLayout, targetLangId, targetHkl, windowHandle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SendInput failed for {ProcessName} → {Layout}.",
                application.ProcessName, targetLayout.LanguageTag);
            return false;
        }
    }

    /// <summary>
    /// Cycles through loaded layouts by pressing Win+Space repeatedly,
    /// checking after each press whether the target was reached.
    /// </summary>
    private bool CycleLayoutsUntilReached(
        ApplicationIdentity application, KeyboardLayout targetLayout,
        int targetLangId, IntPtr targetHkl, IntPtr windowHandle)
    {
        // Get the actual loaded layouts to estimate max cycles.
        var layouts = GetAllLoadedLayouts();
        int maxCycles = layouts?.Length ?? 10; // safety limit

        for (int attempt = 0; attempt < maxCycles; attempt++)
        {
            SendWinSpacePress();

            if (VerifyAfterAttempt(application, targetLayout, targetLangId, windowHandle, maxRetries: 4, retryDelayMs: 100))
            {
                _logger.LogInformation(
                    "SendInput (cycling) reached target for {ProcessName} → {Layout} after {Attempts} press(es).",
                    application.ProcessName, targetLayout.LanguageTag, attempt + 1);
                return true;
            }
        }

        _logger.LogWarning(
            "SendInput (cycling) exhausted {MaxCycles} attempts for {ProcessName} → {Layout}.",
            maxCycles, application.ProcessName, targetLayout.LanguageTag);
        return false;
    }

    // ───────────────────── Atomic Win+Space Key Press ─────────────────────

    /// <summary>
    /// Simulates a single Win+Space press by sending all 4 key events
    /// (Win down, Space down, Space up, Win up) in one atomic <c>SendInput</c> call.
    /// The system recognises the gesture as the Win+Space hotkey and cycles
    /// to the next keyboard layout.
    /// </summary>
    private static void SendWinSpacePress()
    {
        var inputs = new Win32Native.INPUT[4];

        // Win key down
        inputs[0] = new Win32Native.INPUT
        {
            type = Win32Native.INPUT_KEYBOARD,
            ki = new Win32Native.KEYBDINPUT
            {
                wVk = Win32Native.VK_LWIN,
                dwFlags = Win32Native.KEYEVENTF_KEYDOWN,
            }
        };

        // Space key down (Win+Space combo)
        inputs[1] = new Win32Native.INPUT
        {
            type = Win32Native.INPUT_KEYBOARD,
            ki = new Win32Native.KEYBDINPUT
            {
                wVk = Win32Native.VK_SPACE,
                dwFlags = Win32Native.KEYEVENTF_KEYDOWN,
            }
        };

        // Space key up
        inputs[2] = new Win32Native.INPUT
        {
            type = Win32Native.INPUT_KEYBOARD,
            ki = new Win32Native.KEYBDINPUT
            {
                wVk = Win32Native.VK_SPACE,
                dwFlags = Win32Native.KEYEVENTF_KEYUP,
            }
        };

        // Win key up
        inputs[3] = new Win32Native.INPUT
        {
            type = Win32Native.INPUT_KEYBOARD,
            ki = new Win32Native.KEYBDINPUT
            {
                wVk = Win32Native.VK_LWIN,
                dwFlags = Win32Native.KEYEVENTF_KEYUP,
            }
        };

        Win32Native.SendInput(4, inputs, InputSize);
    }

    // ───────────────────── PostMessage Fallback ─────────────────────

    /// <summary>
    /// Sends <c>WM_INPUTLANGCHANGEREQUEST</c> via <c>PostMessage</c> to the
    /// specified or foreground window. Returns <c>true</c> if the layout
    /// actually changed (verified with retries).
    /// </summary>
    private bool TryViaPostMessage(
        ApplicationIdentity application, KeyboardLayout targetLayout,
        int targetLangId, IntPtr windowHandle)
    {
        try
        {
            IntPtr hwnd = windowHandle != IntPtr.Zero
                ? windowHandle
                : Win32Native.GetForegroundWindow();

            if (hwnd == IntPtr.Zero)
            {
                _logger.LogDebug(
                    "PostMessage: no window handle for {ProcessName} → {Layout}.",
                    application.ProcessName, targetLayout.LanguageTag);
                return false;
            }

            string? klId = GetKeyboardLayoutId(targetLayout.LanguageTag);
            if (klId is null)
                return false;

            IntPtr hkl = Win32Native.LoadKeyboardLayout(klId, Win32Native.KLF_SUBSTITUTE_OK);
            if (hkl == IntPtr.Zero)
            {
                _logger.LogDebug(
                    "PostMessage: LoadKeyboardLayout failed for {Layout} (KLID: {KlId}).",
                    targetLayout.LanguageTag, klId);
                return false;
            }

            bool posted = Win32Native.PostMessage(
                hwnd,
                Win32Native.WM_INPUTLANGCHANGEREQUEST,
                wParam: IntPtr.Zero,
                lParam: hkl);

            if (!posted)
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogDebug(
                    "PostMessage failed for HWND 0x{Hwnd:X8}. Error: {Error}",
                    hwnd.ToInt64(), error);
                return false;
            }

            _logger.LogDebug(
                "PostMessage sent to HWND 0x{Hwnd:X8} for {ProcessName} → {Layout} (HKL: 0x{Hkl:X16}).",
                hwnd.ToInt64(), application.ProcessName, targetLayout.LanguageTag, hkl.ToInt64());

            // Verify with retries (PostMessage is async — the target window
            // processes it on its own time).
            return VerifyAfterAttempt(application, targetLayout, targetLangId, hwnd, maxRetries: 5, retryDelayMs: 150);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "PostMessage threw for {ProcessName} → {Layout}.",
                application.ProcessName, targetLayout.LanguageTag);
            return false;
        }
    }

    // ───────────────────── Verification ─────────────────────

    /// <summary>
    /// Verifies the keyboard layout changed to the target by polling
    /// <c>GetKeyboardLayout</c> up to <paramref name="maxRetries"/> times
    /// with <paramref name="retryDelayMs"/> between attempts.
    /// </summary>
    private bool VerifyAfterAttempt(
        ApplicationIdentity application, KeyboardLayout targetLayout,
        int targetLangId, IntPtr windowHandle,
        int maxRetries, int retryDelayMs)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            Thread.Sleep(retryDelayMs);

            int actualLangId = GetLayoutLangId(windowHandle);
            if (actualLangId == targetLangId)
            {
                _logger.LogDebug(
                    "Layout verified for {ProcessName} → {Layout} (LCID 0x{TargetLangId:X4}).",
                    application.ProcessName, targetLayout.LanguageTag, targetLangId);
                return true;
            }

            _logger.LogTrace(
                "Verify attempt {Attempt}/{MaxRetries}: got LCID 0x{ActualLangId:X4}, expected 0x{TargetLangId:X4}.",
                attempt + 1, maxRetries, actualLangId, targetLangId);
        }

        return false;
    }

    /// <summary>
    /// Re-reads the current keyboard layout of the specified or foreground window.
    /// </summary>
    private static int GetLayoutLangId(IntPtr windowHandle)
    {
        IntPtr hwnd = windowHandle != IntPtr.Zero
            ? windowHandle
            : Win32Native.GetForegroundWindow();

        if (hwnd == IntPtr.Zero)
            return 0;

        uint threadId = (uint)Win32Native.GetWindowThreadProcessId(hwnd, out _);
        IntPtr hkl = Win32Native.GetKeyboardLayout(threadId);
        return (int)(hkl.ToInt64() & 0xFFFF);
    }

    /// <summary>
    /// Resolves the target layout's language ID for direct comparison.
    /// </summary>
    private static int GetLayoutLangId(KeyboardLayout targetLayout)
    {
        string? klId = GetKeyboardLayoutId(targetLayout.LanguageTag);
        if (klId is null)
            return 0;

        IntPtr hkl = Win32Native.LoadKeyboardLayout(klId, Win32Native.KLF_SUBSTITUTE_OK);
        if (hkl == IntPtr.Zero)
            return 0;

        return (int)(hkl.ToInt64() & 0xFFFF);
    }

    // ───────────────────── Layout Helpers ─────────────────────

    /// <summary>
    /// Returns all currently loaded keyboard layout HKL handles,
    /// or <c>null</c> on failure.
    /// </summary>
    private static IntPtr[]? GetAllLoadedLayouts()
    {
        int count = Win32Native.GetKeyboardLayoutList(0, null);
        if (count <= 0)
            return null;

        var layouts = new IntPtr[count];
        int written = Win32Native.GetKeyboardLayoutList(count, layouts);
        if (written <= 0)
            return null;

        if (written < count)
            Array.Resize(ref layouts, written);

        return layouts;
    }

    /// <summary>
    /// Converts a language tag (e.g., "en-US") to a keyboard layout ID string (e.g., "00000409").
    /// </summary>
    private static string? GetKeyboardLayoutId(string languageTag)
    {
        try
        {
            var culture = new System.Globalization.CultureInfo(languageTag);
            return $"0000{culture.LCID:X4}";
        }
        catch (System.Globalization.CultureNotFoundException)
        {
            return null;
        }
    }
}
