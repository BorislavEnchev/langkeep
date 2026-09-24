using System.Runtime.InteropServices;
using System.Text;

namespace LangKeep.Infrastructure.Windows.Interop;

/// <summary>
/// Isolated Win32 P/Invoke declarations used by the <c>LangKeep.Infrastructure.Windows</c> layer.
/// No other file in this project should contain P/Invoke definitions.
/// </summary>
internal static class Win32Native
{
    // ───────────────────── Delegates ─────────────────────

    internal delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime);

    // ───────────────────── Constants ─────────────────────

    internal const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    internal const uint EVENT_SYSTEM_FOREGROUND = 0x0003;

    internal const uint WM_INPUTLANGCHANGE = 0x0051;
    internal const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;

    internal const uint SMTO_BLOCK = 0x0001;
    internal const uint SMTO_ABORTIFHUNG = 0x0002;

    internal const int KLF_ACTIVATE = 0x00000001;
    internal const int KLF_SUBSTITUTE_OK = 0x00000002;
    internal const int KLF_SETFORPROCESS = 0x00000100;

    // ───────────────────── SendInput constants ─────────────────────

    internal const uint INPUT_KEYBOARD = 1;
    internal const uint KEYEVENTF_KEYDOWN = 0x0000;
    internal const uint KEYEVENTF_KEYUP = 0x0002;

    internal const ushort VK_LWIN = 0x5B;
    internal const ushort VK_SPACE = 0x20;
    // ───────────────────── Structures ─────────────────────

    [StructLayout(LayoutKind.Sequential)]
    internal struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    /// <summary>
    /// Describes the GUI state (active/focus/capture windows, caret) of a thread's input queue.
    /// Used to locate the window that actually owns keyboard focus, which in multi-thread
    /// UI frameworks (Electron, WebView2, UWP hosts) is often a different thread than the
    /// one owning the foreground top-level window.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct GUITHREADINFO
    {
        public int cbSize;
        public uint flags;
        public IntPtr hwndActive;
        public IntPtr hwndFocus;
        public IntPtr hwndCapture;
        public IntPtr hwndMenuOwner;
        public IntPtr hwndMoveSize;
        public IntPtr hwndCaret;
        public RECT rcCaret;
    }

    /// <summary>
    /// Native INPUT structure. Declared with an explicit layout covering the full
    /// MOUSEINPUT union member so that <c>Marshal.SizeOf&lt;INPUT&gt;()</c> equals the
    /// native size (40 bytes on x64). A keyboard-only declaration yields 32 bytes and
    /// makes <c>SendInput</c> silently reject the call (it returns 0).
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct INPUT
    {
        [FieldOffset(0)]
        public uint type;

        [FieldOffset(8)]
        public KEYBDINPUT ki;

        [FieldOffset(8)]
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // ───────────────────── user32.dll ─────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    internal static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, int flags);

    [DllImport("user32.dll")]
    internal static extern IntPtr LoadKeyboardLayout(string pwszKlid, int flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetKeyboardLayoutName(StringBuilder pwszKlid);

    [DllImport("kernel32.dll")]
    internal static extern int GetCurrentThreadId();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SendMessageTimeout(
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam,
        uint flags,
        uint timeout,
        out IntPtr result);

    [DllImport("user32.dll")]
    internal static extern int GetKeyboardLayoutList(int nBuff, [Out] IntPtr[]? lpList);

    /// <summary>
    /// Retrieves GUI-state information (including the focus window) for the given thread.
    /// Pass <c>0</c> to query the foreground thread.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetGUIThreadInfo(uint idThread, out GUITHREADINFO lpgui);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint SendInput(
        uint cInputs,
        [MarshalAs(UnmanagedType.LPArray)] INPUT[] pInputs,
        int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PostMessage(
        IntPtr hWnd,
        uint Msg,
        IntPtr wParam,
        IntPtr lParam);
}
