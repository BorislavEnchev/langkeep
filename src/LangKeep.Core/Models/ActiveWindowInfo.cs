namespace LangKeep.Core.Models;

/// <summary>
/// A snapshot of the currently active (foreground) window on the system,
/// including its process identity and current keyboard layout.
/// </summary>
public sealed class ActiveWindowInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveWindowInfo"/> class.
    /// </summary>
    public ActiveWindowInfo(
        ApplicationIdentity application,
        KeyboardLayout currentLayout,
        string? windowTitle = null,
        IntPtr windowHandle = default)
    {
        Application = application ?? throw new ArgumentNullException(nameof(application));
        CurrentLayout = currentLayout ?? throw new ArgumentNullException(nameof(currentLayout));
        WindowTitle = windowTitle;
        WindowHandle = windowHandle;
    }

    /// <summary>
    /// Gets the identity of the application that owns the active window.
    /// </summary>
    public ApplicationIdentity Application { get; }

    /// <summary>
    /// Gets the keyboard layout currently active for this window's thread.
    /// </summary>
    public KeyboardLayout CurrentLayout { get; }

    /// <summary>
    /// Gets the window title, if available.
    /// </summary>
    public string? WindowTitle { get; }

    /// <summary>
    /// Gets the native window handle (HWND) of the active window, captured
    /// at the time this snapshot was taken.
    /// </summary>
    public IntPtr WindowHandle { get; }
}
