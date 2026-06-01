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
///   <item><c>SendMessageTimeout</c> with <c>WM_INPUTLANGCHANGEREQUEST</c> to set the target HKL directly.</item>
///   <item><c>SendInput</c> simulating one Win+Space press as a compatibility fallback.</item>
/// </list>
///
/// The primary path is synchronous and uses only a tiny verification window,
/// keeping normal switches responsive while retaining a safe fallback.
/// </summary>
public sealed class Win32KeyboardLayoutSwitcher : IKeyboardLayoutSwitcher
{
    private readonly ILogger<Win32KeyboardLayoutSwitcher> _logger;
    private readonly object _lock = new();

    private static readonly int InputSize = Marshal.SizeOf<Win32Native.INPUT>();
    private const uint DirectSwitchTimeoutMs = 200;
    private const int FastVerifyRetries = 2;
    private const int FastVerifyDelayMs = 20;

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

            // ── Attempt 1: SendMessageTimeout WM_INPUTLANGCHANGEREQUEST ──
            _logger.LogDebug(
                "Attempt 1 — SendMessageTimeout for {ProcessName} → {Layout}.",
                application.ProcessName, targetLayout.LanguageTag);

            if (TryViaSendMessageTimeout(application, targetLayout, targetLangId, windowHandle))
                return true;

            // ── Attempt 2: SendInput Win+Space (atomic) ──
            _logger.LogDebug(
                "Attempt 2 — SendInput Win+Space for {ProcessName} → {Layout}.",
                application.ProcessName, targetLayout.LanguageTag);

            if (TryViaSendInput(application, targetLayout, targetLangId, windowHandle))
                return true;

            _logger.LogWarning(
                "Both SendMessageTimeout and SendInput failed for {ProcessName} → {Layout}.",
                application.ProcessName, targetLayout.LanguageTag);

            return false;
        }
    }

    // ───────────────────── Direct Switch via SendMessageTimeout ─────────────────────

    /// <summary>
    /// Sends <c>WM_INPUTLANGCHANGEREQUEST</c> synchronously to the target window,
    /// requesting a specific loaded keyboard layout instead of cycling layouts.
    /// </summary>
    private bool TryViaSendMessageTimeout(
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
                    "SendMessageTimeout: no window handle for {ProcessName} → {Layout}.",
                    application.ProcessName, targetLayout.LanguageTag);
                return false;
            }

            string? klId = GetKeyboardLayoutId(targetLayout.LanguageTag);
            if (klId is null)
                return false;

            IntPtr hkl = Win32Native.LoadKeyboardLayout(
                klId,
                Win32Native.KLF_ACTIVATE | Win32Native.KLF_SUBSTITUTE_OK);

            if (hkl == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogDebug(
                    "SendMessageTimeout: LoadKeyboardLayout failed for {Layout} (KLID: {KlId}). Error: {Error}",
                    targetLayout.LanguageTag, klId, error);
                return false;
            }

            bool sent = Win32Native.SendMessageTimeout(
                hwnd,
                Win32Native.WM_INPUTLANGCHANGEREQUEST,
                wParam: IntPtr.Zero,
                lParam: hkl,
                flags: Win32Native.SMTO_ABORTIFHUNG | Win32Native.SMTO_BLOCK,
                timeout: DirectSwitchTimeoutMs,
                result: out _);

            if (!sent)
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogDebug(
                    "SendMessageTimeout failed for HWND 0x{Hwnd:X8}. Error: {Error}",
                    hwnd.ToInt64(), error);
                return false;
            }

            _logger.LogDebug(
                "SendMessageTimeout completed for HWND 0x{Hwnd:X8} for {ProcessName} → {Layout} (HKL: 0x{Hkl:X16}).",
                hwnd.ToInt64(), application.ProcessName, targetLayout.LanguageTag, hkl.ToInt64());

            return VerifyQuickly(application, targetLayout, targetLangId, hwnd);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "SendMessageTimeout threw for {ProcessName} → {Layout}.",
                application.ProcessName, targetLayout.LanguageTag);
            return false;
        }
    }

    // ───────────────────── SendInput Win+Space (atomic) ─────────────────────

    /// <summary>
    /// Simulates a single Win+Space press via <c>SendInput</c>.
    /// All 4 key events (Win down, Space down, Space up, Win up) are sent
    /// as a single atomic INPUT array so the OS recognises the hotkey.
    /// This is intentionally a shallow fallback: direct switching is the fast,
    /// deterministic path, while unbounded cycling is slow and can be disruptive.
    /// </summary>
    private bool TryViaSendInput(
        ApplicationIdentity application, KeyboardLayout targetLayout,
        int targetLangId, IntPtr windowHandle)
    {
        try
        {
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

            SendWinSpacePress();

            return VerifyQuickly(application, targetLayout, targetLangId, windowHandle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SendInput failed for {ProcessName} → {Layout}.",
                application.ProcessName, targetLayout.LanguageTag);
            return false;
        }
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

    // ───────────────────── Verification ─────────────────────

    /// <summary>
    /// Verifies the keyboard layout changed to the target with a very short
    /// retry window for apps that update the thread layout just after returning.
    /// </summary>
    private bool VerifyQuickly(
        ApplicationIdentity application, KeyboardLayout targetLayout,
        int targetLangId, IntPtr windowHandle)
    {
        for (int attempt = 0; attempt <= FastVerifyRetries; attempt++)
        {
            int actualLangId = GetLayoutLangId(windowHandle);
            if (actualLangId == targetLangId)
            {
                _logger.LogDebug(
                    "Layout verified for {ProcessName} → {Layout} (LCID 0x{TargetLangId:X4}).",
                    application.ProcessName, targetLayout.LanguageTag, targetLangId);
                return true;
            }

            if (attempt == FastVerifyRetries)
                break;

            _logger.LogTrace(
                "Verify attempt {Attempt}/{MaxRetries}: got LCID 0x{ActualLangId:X4}, expected 0x{TargetLangId:X4}.",
                attempt + 1, FastVerifyRetries + 1, actualLangId, targetLangId);

            Thread.Sleep(FastVerifyDelayMs);
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
