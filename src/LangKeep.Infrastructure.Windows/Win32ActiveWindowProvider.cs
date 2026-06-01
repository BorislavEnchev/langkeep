using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using LangKeep.Core.Interfaces;
using LangKeep.Core.Models;
using LangKeep.Infrastructure.Windows.Interop;
using Microsoft.Extensions.Logging;

namespace LangKeep.Infrastructure.Windows;

/// <summary>
/// Monitors the active (foreground) window on Windows using <c>SetWinEventHook</c>
/// and <c>GetForegroundWindow</c>.
/// </summary>
public sealed class Win32ActiveWindowProvider : IActiveWindowProvider, IDisposable
{
    private readonly ILogger<Win32ActiveWindowProvider> _logger;
    private readonly object _hookLock = new();

    private Win32Native.WinEventDelegate? _winEventDelegate;
    private IntPtr _foregroundHook = IntPtr.Zero;
    private bool _disposed;
    private ActiveWindowInfo? _lastExternalActiveWindow;

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32ActiveWindowProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public Win32ActiveWindowProvider(ILogger<Win32ActiveWindowProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ActiveWindowInfo? LastExternalActiveWindow => _lastExternalActiveWindow;

    /// <inheritdoc />
    public event EventHandler<ActiveWindowInfo>? ActiveWindowChanged;

    /// <summary>
    /// Registers the WinEvent hook for foreground window changes.
    /// </summary>
    /// <returns><c>true</c> if the hook was registered successfully.</returns>
    public bool StartHook()
    {
        lock (_hookLock)
        {
            if (_foregroundHook != IntPtr.Zero)
                return true;

            _winEventDelegate = WinEventProc;
            _foregroundHook = Win32Native.SetWinEventHook(
                Win32Native.EVENT_SYSTEM_FOREGROUND,
                Win32Native.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                _winEventDelegate,
                0, 0,
                Win32Native.WINEVENT_OUTOFCONTEXT);

            if (_foregroundHook == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogError("SetWinEventHook failed with error code {Error}.", error);
                return false;
            }

            _logger.LogInformation("Foreground window event hook registered.");
            return true;
        }
    }

    /// <summary>
    /// Unregisters the WinEvent hook.
    /// </summary>
    public void StopHook()
    {
        lock (_hookLock)
        {
            if (_foregroundHook != IntPtr.Zero)
            {
                Win32Native.UnhookWinEvent(_foregroundHook);
                _foregroundHook = IntPtr.Zero;
                _logger.LogInformation("Foreground window event hook unregistered.");
            }
        }
    }

    /// <inheritdoc />
    public ActiveWindowInfo? GetActiveWindow()
    {
        try
        {
            IntPtr hwnd = Win32Native.GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return null;

            return GetWindowInfo(hwnd);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get active window info.");
            return null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            StopHook();
            _disposed = true;
        }
    }

    // ───────────────────── Event Callback ─────────────────────

    private void WinEventProc(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        if (hwnd == IntPtr.Zero)
            return;

        try
        {
            var info = GetWindowInfo(hwnd, dwEventThread);
            if (info is not null)
            {
                _logger.LogDebug(
                    "Foreground changed: {ProcessName} / {Layout}",
                    info.Application.ProcessName,
                    info.CurrentLayout.LanguageTag);

                ActiveWindowChanged?.Invoke(this, info);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error in WinEventProc for foreground change.");
        }
    }

    // ───────────────────── Window Info Helper ─────────────────────

    private ActiveWindowInfo? GetWindowInfo(IntPtr hwnd, uint? eventThreadId = null)
    {
        try
        {
            // Window title
            int length = Win32Native.GetWindowTextLength(hwnd);
            if (length < 0)
                return null;

            var titleSb = new StringBuilder(length + 1);
            Win32Native.GetWindowText(hwnd, titleSb, titleSb.Capacity);
            string title = titleSb.ToString().Trim();

            // Process & thread
            uint threadIdFromApi = (uint)Win32Native.GetWindowThreadProcessId(hwnd, out uint pid);
            uint threadId = eventThreadId ?? threadIdFromApi;

            // Process name
            string processName = GetProcessName(pid);

            // Keyboard layout
            IntPtr hkl = Win32Native.GetKeyboardLayout(threadId);
            int langId = (int)(hkl.ToInt64() & 0xFFFF);
            var layout = KeyboardLayout.FromLcid(langId);

            var application = new ApplicationIdentity(
                processName,
                processPath: null,
                windowTitle: title);

            var info = new ActiveWindowInfo(application, layout, windowTitle: title, windowHandle: hwnd);

            if (pid != (uint)Process.GetCurrentProcess().Id)
            {
                _lastExternalActiveWindow = info;
            }

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get window info for HWND {Hwnd}.", hwnd);
            return null;
        }
    }

    private static string GetProcessName(uint pid)
    {
        try
        {
            using var proc = Process.GetProcessById((int)pid);
            return proc.ProcessName + ".exe";
        }
        catch (ArgumentException)
        {
            return $"PID({pid})";
        }
    }
}
