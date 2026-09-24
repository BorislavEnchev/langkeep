using LangKeep.Core.Interfaces;
using LangKeep.Core.Models;
using LangKeep.Infrastructure.Windows.Interop;
using Microsoft.Extensions.Logging;

namespace LangKeep.Infrastructure.Windows;

/// <summary>
/// Monitors keyboard layout changes on Windows by polling <c>GetKeyboardLayout</c>
/// for the current foreground window's thread.
/// </summary>
public sealed class Win32KeyboardLayoutProvider : IKeyboardLayoutProvider, IDisposable
{
    private readonly ILogger<Win32KeyboardLayoutProvider> _logger;
    private readonly IActiveWindowProvider _activeWindowProvider;

    private int _previousLayoutLangId;
    private Timer? _pollingTimer;
    private bool _disposed;

    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32KeyboardLayoutProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="activeWindowProvider">The active window provider (used to get the current thread ID).</param>
    public Win32KeyboardLayoutProvider(
        ILogger<Win32KeyboardLayoutProvider> logger,
        IActiveWindowProvider activeWindowProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activeWindowProvider = activeWindowProvider ?? throw new ArgumentNullException(nameof(activeWindowProvider));
    }

    /// <inheritdoc />
    public event EventHandler<KeyboardLayout>? LayoutChanged;

    /// <summary>
    /// Starts the polling timer to detect layout changes.
    /// </summary>
    public void StartPolling()
    {
        // Initialize the previous layout to the current one so the first poll
        // doesn't falsely trigger a layout-change event.
        _previousLayoutLangId = GetCurrentForegroundLangId();

        _pollingTimer = new Timer(
            _ => CheckLayout(),
            null,
            PollingInterval,
            PollingInterval);

        _logger.LogDebug("Keyboard layout polling started (interval: {IntervalMs} ms).", PollingInterval.TotalMilliseconds);
    }

    /// <summary>
    /// Stops the polling timer.
    /// </summary>
    public void StopPolling()
    {
        _pollingTimer?.Dispose();
        _pollingTimer = null;
        _logger.LogDebug("Keyboard layout polling stopped.");
    }

    /// <inheritdoc />
    public KeyboardLayout? GetCurrentLayout(ApplicationIdentity application)
    {
        try
        {
            IntPtr hwnd = Win32Native.GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return null;

            int langId = GetCurrentForegroundLangId();
            return KeyboardLayout.FromLcid(langId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get current layout for {ProcessName}.", application.ProcessName);
            return null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            StopPolling();
            _disposed = true;
        }
    }

    // ───────────────────── Polling ─────────────────────

    private void CheckLayout()
    {
        try
        {
            int langId = GetCurrentForegroundLangId();
            if (langId == 0)
                return;

            if (langId != _previousLayoutLangId)
            {
                _previousLayoutLangId = langId;
                var layout = KeyboardLayout.FromLcid(langId);

                _logger.LogDebug("Layout change detected: {Layout}.", layout.LanguageTag);
                LayoutChanged?.Invoke(this, layout);
            }
        }
        catch (Exception ex)
        {
            // Swallow transient failures; polling will retry.
            _logger.LogTrace(ex, "Transient error in layout polling.");
        }
    }

    /// <summary>
    /// Reads the low-word language id of the keyboard layout for the thread that
    /// currently owns keyboard input.
    /// <para>
    /// Multi-threaded UI frameworks (Electron, WebView2 — e.g. the new Teams,
    /// <c>msteams.exe</c>) host keyboard focus on a different thread than the one that
    /// owns the top-level foreground window. Reading the layout from the window-owning
    /// thread returns a stale value there, so the thread that actually owns keyboard
    /// input is resolved via <see cref="KeyboardInputThread"/>.
    /// </para>
    /// </summary>
    /// <returns>The low-word language id, or 0 when it cannot be determined.</returns>
    private static int GetCurrentForegroundLangId()
    {
        IntPtr hwnd = Win32Native.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return 0;

        uint windowThreadId = (uint)Win32Native.GetWindowThreadProcessId(hwnd, out _);
        uint inputThreadId = KeyboardInputThread.Resolve(hwnd, windowThreadId, out _);
        return KeyboardInputThread.GetLayoutLangId(inputThreadId);
    }
}
