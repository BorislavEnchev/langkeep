using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace LangKeep.Spike;

/// <summary>
/// Monitors active (foreground) window changes and keyboard layout switches
/// on Windows using Win32 event hooks.
/// </summary>
internal class Program
{
    // ───────────────────── Win32 Delegates ─────────────────────

    private delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime);

    // ───────────────────── Win32 Constants ─────────────────────

    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;

    // ───────────────────── Win32 P/Invoke ─────────────────────

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int GetCurrentThreadId();

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    // ───────────────────── State ─────────────────────

    /// <summary>
    /// Must keep a strong reference to prevent GC from collecting the delegate.
    /// </summary>
    private static WinEventDelegate? _winEventDelegate;

    private static IntPtr _foregroundHook = IntPtr.Zero;

    // Cached thread ID of the current foreground window (used by the polling timer).
    private static uint _currentForegroundThreadId;

    // The most-recently observed keyboard layout language ID (LCID from HKL low-word).
    private static int _previousLayoutLangId;

    // ── JSON file logger ──
    private static JsonEventLogger? _logger;

    // ───────────────────── Entry Point ─────────────────────

    private static void Main()
    {
        // ── Initialize JSON file logger ──
        _logger = new JsonEventLogger();

        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           LangKeep.Spike — Window & Layout Monitor      ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine($"Started : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        Console.WriteLine($"PID     : {Environment.ProcessId}");
        Console.WriteLine($"Thread  : {GetCurrentThreadId()}");
        Console.WriteLine($"Log file: {_logger.FilePath}");
        Console.WriteLine();
        Console.WriteLine("Monitoring foreground window changes and keyboard layout switches.");
        Console.WriteLine("Press Ctrl+C to exit.");
        Console.WriteLine(new string('─', 70));

        // ── Log application start ──
        _logger.LogStart(Environment.ProcessId, GetCurrentThreadId());

        // ── Register foreground event hook ──
        _winEventDelegate = WinEventProc;
        _foregroundHook = SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND,
            EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _winEventDelegate,
            0, 0,
            WINEVENT_OUTOFCONTEXT);

        if (_foregroundHook == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            Console.Error.WriteLine($"[FATAL] SetWinEventHook failed. Error code: {error}");
            Environment.Exit(1);
        }

        // Capture initial foreground window state
        CaptureCurrentForegroundWindow();

        // ── Layout-change polling timer (every 500 ms) ──
        using var layoutTimer = new Timer(
            _ => CheckKeyboardLayout(),
            null,
            500,   // start after 500 ms
            500);  // repeat every 500 ms

        // ── Clean shutdown on Ctrl+C ──
        Console.CancelKeyPress += (_, args) =>
        {
            args.Cancel = true;
            Cleanup();
            Environment.Exit(0);
        };

        // ── Message pump (required by WinEvent hooks) ──
        RunMessagePump();
    }

    // ───────────────────── Message Pump ─────────────────────

    private static void RunMessagePump()
    {
        while (GetMessage(out MSG msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        // If GetMessage returns ≤ 0 we should clean up.
        Cleanup();
    }

    // ───────────────────── Foreground Event Callback ─────────────────────

    private static void WinEventProc(
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

        var info = GetActiveWindowInfo(hwnd, dwEventThread);
        if (info == null)
            return;

        _currentForegroundThreadId = info.ThreadId;
        _previousLayoutLangId = info.LayoutLangId;

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ═══ FOREGROUND WINDOW CHANGED ═══");
        PrintWindowInfo(info);
        Console.WriteLine();

        _logger?.LogForegroundChange(info);
    }

    // ───────────────────── Polling Timer ─────────────────────

    private static void CheckKeyboardLayout()
    {
        if (_currentForegroundThreadId == 0)
            return;

        IntPtr hkl = GetKeyboardLayout(_currentForegroundThreadId);
        int langId = (int)(hkl.ToInt64() & 0xFFFF);

        if (langId != _previousLayoutLangId)
        {
            _previousLayoutLangId = langId;
            string layoutName = GetLayoutDisplayName(langId);

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ═══ KEYBOARD LAYOUT CHANGED ═══");
            Console.WriteLine($"  New Layout : {layoutName}  (LCID: 0x{langId:X4} / HKL: 0x{hkl.ToInt64():X8})");
            Console.WriteLine();

            _logger?.LogLayoutChange(layoutName, langId, hkl.ToInt64());
        }
    }

    // ───────────────────── Helpers ─────────────────────

    private static void CaptureCurrentForegroundWindow()
    {
        // Use GetForegroundWindow to grab the current foreground window
        IntPtr hwnd = GetForegroundWindow();
        if (hwnd != IntPtr.Zero)
        {
            uint threadId = (uint)GetWindowThreadProcessId(hwnd, out _);
            var info = GetActiveWindowInfo(hwnd, threadId);
            if (info != null)
            {
                _currentForegroundThreadId = info.ThreadId;
                _previousLayoutLangId = info.LayoutLangId;

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ═══ INITIAL STATE ═══");
                PrintWindowInfo(info);
                Console.WriteLine();

                _logger?.LogInitialState(info);
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private static ActiveWindowInfo? GetActiveWindowInfo(IntPtr hwnd, uint eventThreadId)
    {
        // ── Window title ──
        int length = GetWindowTextLength(hwnd);
        if (length < 0)
            return null;
        var titleSb = new StringBuilder(length + 1);
        GetWindowText(hwnd, titleSb, titleSb.Capacity);
        string title = titleSb.ToString().Trim();

        // ── Process & thread ──
        // GetWindowThreadProcessId returns the thread ID as its return value
        uint threadIdFromApi = (uint)GetWindowThreadProcessId(hwnd, out uint pid);
        uint threadId = eventThreadId > 0 ? eventThreadId : threadIdFromApi;

        string processName = "Unknown";
        try
        {
            using var proc = Process.GetProcessById((int)pid);
            processName = proc.ProcessName;
        }
        catch (ArgumentException)
        {
            processName = $"PID({pid})";
        }

        // ── Keyboard layout (HKL) ──
        IntPtr hkl = GetKeyboardLayout(threadId);
        int langId = (int)(hkl.ToInt64() & 0xFFFF);
        string layoutName = GetLayoutDisplayName(langId);

        return new ActiveWindowInfo
        {
            Hwnd = hwnd,
            Title = title,
            ProcessName = processName,
            Pid = pid,
            ThreadId = threadId,
            Hkl = hkl,
            LayoutLangId = langId,
            LayoutDisplayName = layoutName,
        };
    }

    private static string GetLayoutDisplayName(int langId)
    {
        try
        {
            var culture = new CultureInfo(langId);
            return $"{culture.DisplayName}  [{culture.Name}]";
        }
        catch (CultureNotFoundException)
        {
            return $"Unknown (0x{langId:X4})";
        }
    }

    private static void PrintWindowInfo(ActiveWindowInfo info)
    {
        Console.WriteLine($"  Window Title : {info.Title}");
        Console.WriteLine($"  Process      : {info.ProcessName} (PID: {info.Pid})");
        Console.WriteLine($"  Thread       : {info.ThreadId}");
        Console.WriteLine($"  Keyboard     : {info.LayoutDisplayName}  (HKL: 0x{info.Hkl.ToInt64():X8})");
    }

    // ───────────────────── Cleanup ─────────────────────

    private static void Cleanup()
    {
        if (_foregroundHook != IntPtr.Zero)
        {
            UnhookWinEvent(_foregroundHook);
            _foregroundHook = IntPtr.Zero;
        }

        Console.WriteLine(new string('─', 70));
        Console.WriteLine($"Shutdown at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

        _logger?.LogShutdown();
        _logger?.Dispose();
    }

    // ───────────────────── Data Model ─────────────────────

    private sealed class ActiveWindowInfo
    {
        public IntPtr Hwnd { get; init; }
        public string Title { get; init; } = string.Empty;
        public string ProcessName { get; init; } = string.Empty;
        public uint Pid { get; init; }
        public uint ThreadId { get; init; }
        public IntPtr Hkl { get; init; }
        public int LayoutLangId { get; init; }
        public string LayoutDisplayName { get; init; } = string.Empty;
    }

    // ───────────────────── JSON File Logger ─────────────────────

    /// <summary>
    /// Writes structured events to a newline-delimited JSON (NDJSON) log file.
    /// Thread-safe via a lock around each write.
    /// </summary>
    private sealed class JsonEventLogger : IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly object _lock = new();

        public string FilePath { get; }

        public JsonEventLogger()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            FilePath = Path.Combine(exeDir, $"langkeep-spike-{timestamp}.ndjson");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            _writer = new StreamWriter(FilePath, append: false, encoding: Encoding.UTF8)
            {
                AutoFlush = true,
            };
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _writer.Flush();
                _writer.Dispose();
            }
        }

        // ── Write helper ──

        private void WriteEvent(string eventType, object data)
        {
            var root = new Dictionary<string, object?>
            {
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["eventType"] = eventType,
                ["data"] = data,
            };
            string json = JsonSerializer.Serialize(root, _jsonOptions);
            lock (_lock)
            {
                _writer.WriteLine(json);
            }
        }

        // ── Public log methods ──

        public void LogStart(int processId, int threadId)
        {
            var data = new { pid = processId, threadId };
            WriteEvent("start", data);
        }

        public void LogInitialState(ActiveWindowInfo info)
        {
            var data = new
            {
                windowTitle = info.Title,
                processName = info.ProcessName,
                pid = info.Pid,
                threadId = info.ThreadId,
                keyboardLayout = info.LayoutDisplayName,
                hkl = $"0x{info.Hkl.ToInt64():X8}",
            };
            WriteEvent("initial_state", data);
        }

        public void LogForegroundChange(ActiveWindowInfo info)
        {
            var data = new
            {
                windowTitle = info.Title,
                processName = info.ProcessName,
                pid = info.Pid,
                threadId = info.ThreadId,
                keyboardLayout = info.LayoutDisplayName,
                hkl = $"0x{info.Hkl.ToInt64():X8}",
            };
            WriteEvent("foreground_change", data);
        }

        public void LogLayoutChange(string layoutName, int langId, long hkl)
        {
            var data = new
            {
                keyboardLayout = layoutName,
                langId,
                hkl = $"0x{hkl:X8}",
            };
            WriteEvent("layout_change", data);
        }

        public void LogShutdown()
        {
            WriteEvent("shutdown", new { });
        }
    }
}
