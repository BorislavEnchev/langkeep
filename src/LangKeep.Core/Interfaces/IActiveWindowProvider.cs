using LangKeep.Core.Models;

namespace LangKeep.Core.Interfaces;

/// <summary>
/// Provides information about the currently active (foreground) window.
/// </summary>
/// <remarks>
/// Platform-specific implementations (e.g., Win32, macOS) live in their own
/// infrastructure projects and implement this interface.
/// </remarks>
public interface IActiveWindowProvider
{
    /// <summary>
    /// Gets a snapshot of the currently active window, including its process
    /// identity and current keyboard layout.
    /// </summary>
    /// <returns>
    /// An <see cref="ActiveWindowInfo"/> for the active window, or <c>null</c>
    /// if the active window cannot be determined (e.g., desktop, lock screen).
    /// </returns>
    ActiveWindowInfo? GetActiveWindow();

    /// <summary>
    /// Occurs when the active (foreground) window changes.
    /// </summary>
    event EventHandler<ActiveWindowInfo>? ActiveWindowChanged;
}
