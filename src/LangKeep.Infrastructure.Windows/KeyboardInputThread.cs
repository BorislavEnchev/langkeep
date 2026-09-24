using System.Runtime.InteropServices;
using LangKeep.Infrastructure.Windows.Interop;

namespace LangKeep.Infrastructure.Windows;

/// <summary>
/// Resolves the thread that actually owns keyboard input for the foreground window.
/// </summary>
/// <remarks>
/// Traditional Win32 apps create the foreground window and its input focus on the same
/// thread, so <c>GetKeyboardLayout(foregroundThread)</c> reflects what the user types.
/// Multi-threaded UI frameworks (Electron, WebView2 — e.g. the new Microsoft Teams,
/// which moved from <c>teams.exe</c> to a packaged <c>msteams.exe</c>) host keyboard
/// focus on a <em>different</em> thread than the one owning the top-level window.
/// Reading the layout from the window-owning thread then returns a stale value, which
/// breaks both layout-change detection (learning) and switch verification.
/// <para>
/// <c>GetGUIThreadInfo</c> reports the window that owns keyboard focus for the
/// foreground input queue; the layout must be read from <em>that</em> thread.
/// </para>
/// </remarks>
internal static class KeyboardInputThread
{
    /// <summary>
    /// Returns the thread id whose <c>GetKeyboardLayout</c> reflects the layout the user
    /// is typing with, and (when available) the window that owns keyboard focus.
    /// </summary>
    /// <remarks>
    /// The focus window may belong to a <em>different process</em> than the foreground
    /// window: the new Microsoft Teams (<c>msteams.exe</c>) hosts its UI — and therefore
    /// keyboard focus — inside <c>msedgewebview2.exe</c> child processes. The focus
    /// thread is still authoritative for the layout <em>value</em> because it belongs to
    /// the foreground input queue, i.e. it is by definition where user keystrokes go.
    /// Application attribution is unrelated to this and stays derived from the
    /// foreground window's process.
    /// </remarks>
    /// <param name="foregroundHwnd">Handle of the foreground (top-level) window.</param>
    /// <param name="fallbackThreadId">
    /// Thread id to use when the GUI state cannot be queried — typically the
    /// window-owning thread.
    /// </param>
    /// <param name="focusHwnd">
    /// Receives the window that owns keyboard focus, or <see cref="IntPtr.Zero"/> when
    /// it could not be determined.
    /// </param>
    /// <returns>The thread id to read the keyboard layout from.</returns>
    public static uint Resolve(IntPtr foregroundHwnd, uint fallbackThreadId, out IntPtr focusHwnd)
    {
        focusHwnd = IntPtr.Zero;

        if (foregroundHwnd == IntPtr.Zero)
            return fallbackThreadId;

        uint foregroundPid = 0;
        uint foregroundThreadId = (uint)Win32Native.GetWindowThreadProcessId(foregroundHwnd, out foregroundPid);
        if (foregroundThreadId == 0)
            return fallbackThreadId;

        // Query the GUI state of the foreground input queue: first scoped to the
        // window-owning thread, then queue-wide (idThread = 0). Both report the window
        // that holds keyboard focus for the queue receiving user input.
        if (!TryGetFocusWindow(foregroundThreadId, out IntPtr focusCandidate) &&
            !TryGetFocusWindow(0, out focusCandidate))
        {
            // No GUI state available — the window-owning thread is the best we can do.
            return foregroundThreadId;
        }

        if (focusCandidate == IntPtr.Zero)
            return foregroundThreadId;

        uint focusPid = 0;
        uint focusThreadId = (uint)Win32Native.GetWindowThreadProcessId(focusCandidate, out focusPid);
        if (focusThreadId == 0)
            return foregroundThreadId;

        // Sanity check: the focus thread must report a usable keyboard layout.
        // Threads can report HKL 0 (no input queue) — treat those as unreliable.
        IntPtr hkl = Win32Native.GetKeyboardLayout(focusThreadId);
        if (hkl == IntPtr.Zero || (hkl.ToInt64() & 0xFFFF) == 0)
            return foregroundThreadId;

        focusHwnd = focusCandidate;
        return focusThreadId;
    }

    /// <summary>
    /// Reads the low-word language id of the keyboard layout for a thread
    /// (0 when the thread reports no layout).
    /// </summary>
    public static int GetLayoutLangId(uint threadId)
    {
        IntPtr hkl = Win32Native.GetKeyboardLayout(threadId);
        return (int)(hkl.ToInt64() & 0xFFFF);
    }

    private static bool TryGetFocusWindow(uint threadId, out IntPtr focusHwnd)
    {
        focusHwnd = IntPtr.Zero;

        var info = new Win32Native.GUITHREADINFO
        {
            cbSize = Marshal.SizeOf<Win32Native.GUITHREADINFO>(),
        };

        if (!Win32Native.GetGUIThreadInfo(threadId, out info))
            return false;

        focusHwnd = info.hwndFocus;
        return true;
    }
}
