using LangKeep.Core.Models;

namespace LangKeep.Core.Interfaces;

/// <summary>
/// Provides the ability to programmatically switch the keyboard layout
/// for a given application or thread.
/// </summary>
public interface IKeyboardLayoutSwitcher
{
    /// <summary>
    /// Attempts to switch the active window's keyboard layout to the specified one.
    /// </summary>
    /// <param name="application">The target application identity.</param>
    /// <param name="targetLayout">The desired keyboard layout.</param>
    /// <param name="windowHandle">
    /// Optional native window handle (HWND). When provided, the switcher will use
    /// this handle directly instead of querying <c>GetForegroundWindow()</c>.
    /// </param>
    /// <returns><c>true</c> if the switch succeeded; otherwise, <c>false</c>.</returns>
    bool TrySwitchLayout(ApplicationIdentity application, KeyboardLayout targetLayout, IntPtr windowHandle = default);
}
