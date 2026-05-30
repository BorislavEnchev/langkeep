using LangKeep.Core.Models;

namespace LangKeep.Core.Interfaces;

/// <summary>
/// Provides the current keyboard layout for a given application or thread.
/// </summary>
public interface IKeyboardLayoutProvider
{
    /// <summary>
    /// Gets the current keyboard layout for the specified <see cref="ApplicationIdentity"/>.
    /// </summary>
    /// <param name="application">The application identity.</param>
    /// <returns>The current keyboard layout, or <c>null</c> if it cannot be determined.</returns>
    KeyboardLayout? GetCurrentLayout(ApplicationIdentity application);

    /// <summary>
    /// Occurs when the keyboard layout changes for the active window.
    /// </summary>
    event EventHandler<KeyboardLayout>? LayoutChanged;
}
