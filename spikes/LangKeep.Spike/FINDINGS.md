# LangKeep.Spike — Findings Document

## Overview

This spike explores how to build a .NET 9 console application that monitors active (foreground) window changes and keyboard layout switches on Windows. It uses Win32 event hooks (via P/Invoke) combined with a polling timer for layout detection.

---

## Approach

### 1. Foreground Window Detection

Windows provides a **WinEvent** hooking mechanism (`SetWinEventHook`) that allows applications to subscribe to system-wide UI automation events without polling.

| API | Purpose |
|-----|---------|
| `SetWinEventHook(EVENT_SYSTEM_FOREGROUND, ...)` | Registers a callback that fires whenever the foreground window changes |
| `UnhookWinEvent(...)` | Unregisters the hook on shutdown |
| `GetWindowText(hwnd, ...)` | Retrieves the title of the window |
| `GetWindowThreadProcessId(hwnd, out pid)` | Gets both the PID and the thread ID that owns the window |
| `GetForegroundWindow()` | Grabs the current foreground window handle (used once at startup) |

**Key constraints:**
- The callback delegate **must** be stored in a class-level variable; otherwise the GC will collect it and cause an `AccessViolationException` when Windows tries to invoke the hook.
- A **Windows message pump** (a `GetMessage`/`TranslateMessage`/`DispatchMessage` loop) is required for WinEvent hooks to deliver callbacks. We implemented a manual message pump via P/Invoke rather than depending on WinForms/WPF.

### 2. Keyboard Layout Detection

Two complementary mechanisms:

| Mechanism | How |
|-----------|-----|
| **On window change** | When `WinEventProc` fires, call `GetKeyboardLayout(threadId)` for the foreground window's thread |
| **Changes between window switches** | A `System.Threading.Timer` polls `GetKeyboardLayout(currentForegroundThreadId)` every **500 ms** and logs any change in the language ID |

The **HKL** (keyboard layout handle) is structured as:
- **Low word** (bits 0–15): Language Identifier (LCID) — e.g. `0x0409` for en-US, `0x0419` for ru-RU.
- **High word** (bits 16–31): Device identifier (usually 0 for software layouts).

The LCID is converted to a human-readable name via `System.Globalization.CultureInfo(langId).DisplayName`.

### 3. Message Pump (No WinForms Dependency)

Since this is a pure console application, we avoid adding the `System.Windows.Forms` package by implementing our own message pump:

```csharp
[DllImport("user32.dll")]
private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

while (GetMessage(out MSG msg, IntPtr.Zero, 0, 0) > 0)
{
    TranslateMessage(ref msg);
    DispatchMessage(ref msg);
}
```

This keeps the project dependency-free and lightweight.

### 4. Structured JSON File Logging (NDJSON)

Every event is also persisted to a **newline-delimited JSON (NDJSON)** log file alongside the console output. Each line is a self-contained JSON object with a UTC timestamp, event type, and event-specific data payload.

| Aspect | Detail |
|--------|--------|
| **File name** | `langkeep-spike-{yyyyMMdd-HHmmss}.ndjson` (one file per session) |
| **Location** | Same directory as the executable (`AppDomain.CurrentDomain.BaseDirectory`) |
| **Format** | Newline-delimited JSON (NDJSON) — each line is a valid JSON object |
| **Serialization** | `System.Text.Json` (built into .NET, no extra packages) |
| **Thread safety** | All writes are guarded by a lock since the logger is accessed from multiple threads |

**Event types emitted:**

| Event Type | Trigger | Data Fields |
|------------|---------|-------------|
| `start` | Application startup | `pid`, `threadId` |
| `initial_state` | First window snapshot | `windowTitle`, `processName`, `pid`, `threadId`, `keyboardLayout`, `hkl` |
| `foreground_change` | User switches to a different window | `windowTitle`, `processName`, `pid`, `threadId`, `keyboardLayout`, `hkl` |
| `layout_change` | Keyboard layout switches (polled) | `keyboardLayout`, `langId`, `hkl` |
| `shutdown` | Application exits | *(empty)* |

**Example NDJSON output:**
```jsonl
{"timestamp":"2026-05-30T10:34:56.7890000Z","eventType":"start","data":{"pid":12345,"threadId":1}}
{"timestamp":"2026-05-30T10:34:56.8000000Z","eventType":"initial_state","data":{"windowTitle":"Console Window Host","processName":"conhost","pid":12345,"threadId":1234,"keyboardLayout":"English (United States) [en-US]","hkl":"0x04090409"}}
{"timestamp":"2026-05-30T10:34:59.1230000Z","eventType":"foreground_change","data":{"windowTitle":"script.py — VS Code","processName":"Code","pid":9876,"threadId":5678,"keyboardLayout":"English (United States) [en-US]","hkl":"0x04090409"}}
{"timestamp":"2026-05-30T10:35:02.4560000Z","eventType":"layout_change","data":{"keyboardLayout":"Russian (Russia) [ru]","langId":1049,"hkl":"0x04190409"}}
{"timestamp":"2026-05-30T10:35:10.0000000Z","eventType":"shutdown","data":{}}
```

The NDJSON format is easy to consume from scripts, ELK stacks, or `System.Text.Json` streaming readers (`Utf8JsonReader`).

---

## Challenges & Observations

### Challenge 1: WinEvent Hooks Require a Message Pump

`SetWinEventHook` with the `WINEVENT_OUTOFCONTEXT` flag still requires the calling thread to pump messages. Without a message loop, the callback never fires. A simple `Console.ReadLine()` is **not sufficient**.

**Resolution:** Use a manual `GetMessage` loop (see above). This is the standard approach for Windows console applications that need window event hooks.

### Challenge 2: Keyboard Layout Changes Are Subtle

The `EVENT_SYSTEM_FOREGROUND` event fires when the user Alt+Tabs or clicks a different window, but it does **not** fire when the user switches keyboard layouts (e.g., Win+Space or Alt+Shift) while staying in the same window. There is no dedicated WinEvent for keyboard layout changes.

**Resolution:** Added a **polling timer** that checks the current layout every 500 ms. This is a pragmatic trade-off — polling is slightly less responsive but simple and reliable.

### Challenge 3: Thread-Safe Access to Foreground Thread ID

The `WinEventProc` callback runs on the thread that owns the foreground window's message queue (not the main thread), while the timer callback runs on the thread-pool. Accessing `_currentForegroundThreadId` from both paths is technically racy, but in practice the race window is negligible for monitoring purposes.

### Observation 4: HKL → LCID → CultureInfo Mapping (Language Only)

The low word of the HKL handle conveniently maps to a Windows LCID (language ID), which `CultureInfo` can parse:

```csharp
int langId = (int)(hkl.ToInt64() & 0xFFFF);
var culture = new CultureInfo(langId);
```

This yields display names like *"English (United States) [en-US]"*.

**Known limitation:** The LCID identifies the **language** (e.g., "English") but not the **specific keyboard layout variant** (e.g., "US" vs "US International" vs "Dvorak"). The exact keyboard variant is encoded in the high word of HKL or via the KLID string from `GetKeyboardLayoutName`. For a spike where the primary interest is language detection (e.g., tracking when a user switches between Russian and English input), this approach is sufficient.

### Observation 5: Process Name from PID

`Process.GetProcessById(pid).ProcessName` works reliably but throws `ArgumentException` if the process exits between the hook call and the lookup (hence the `try/catch`).

---

## Sample Console Output

```
╔══════════════════════════════════════════════════════════╗
║           LangKeep.Spike — Window & Layout Monitor      ║
╚══════════════════════════════════════════════════════════╝
Started : 2026-05-30 12:34:56.789
PID     : 12345
Thread  : 1

Monitoring foreground window changes and keyboard layout switches.
Press Ctrl+C to exit.
──────────────────────────────────────────────────────────────
[12:34:56.800] ═══ INITIAL STATE ═══
  Window Title : Console Window Host
  Process      : conhost (PID: 12345)
  Thread       : 1234
  Keyboard     : English (United States) [en-US]  (HKL: 0x04090409)

[12:34:59.123] ═══ FOREGROUND WINDOW CHANGED ═══
  Window Title : script.py — VS Code
  Process      : Code (PID: 9876)
  Thread       : 5678
  Keyboard     : English (United States) [en-US]  (HKL: 0x04090409)

[12:35:02.456] ═══ KEYBOARD LAYOUT CHANGED ═══
  New Layout : Russian (Russia) [ru]  (LCID: 0x0419 / HKL: 0x04190409)
```

---

## API Reference

| API | Library | Notes |
|-----|---------|-------|
| `SetWinEventHook` | `user32.dll` | Requires message pump |
| `UnhookWinEvent` | `user32.dll` | Call on shutdown |
| `GetWindowText` | `user32.dll` | Retrieves window title |
| `GetWindowTextLength` | `user32.dll` | Gets exact buffer size needed |
| `GetWindowThreadProcessId` | `user32.dll` | Returns both thread ID and PID |
| `GetKeyboardLayout` | `user32.dll` | Pass thread ID; returns HKL |
| `GetForegroundWindow` | `user32.dll` | Current foreground window handle |
| `GetMessage` / `TranslateMessage` / `DispatchMessage` | `user32.dll` | Standard Win32 message pump |
| `Process.GetProcessById` | `System.Diagnostics` | .NET BCL — may throw if process exits |
| `CultureInfo` | `System.Globalization` | Converts LCID to display name |
| `JsonSerializer` | `System.Text.Json` | Built-in JSON serialization for NDJSON log file |
| `StreamWriter` | `System.IO` | Writes NDJSON lines to disk |

---

## Potential Improvements

1. **Higher layout responsiveness** — Replace polling with `SetWindowsHookEx(WH_KEYBOARD_LL)` to detect hotkey presses (Alt+Shift, Win+Space) that trigger layout switches, then immediately check `GetKeyboardLayout`.
2. **WPF/WinForms host** — A GUI app gets the message pump for free via `Application.Run()` and offers a richer UX for the monitoring tool.
3. **Layout change events** — Subscribe to `EVENT_OBJECT_FOCUS` to catch input language changes on individual UI elements within the same window.
4. ~~**Log to file** — Add file-based logging (structured JSON) for long-running sessions.~~ ✅ **[Implemented]**
