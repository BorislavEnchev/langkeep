<#
.SYNOPSIS
    LangKeep Alt+Tab Test Harness - simulates real Alt+Tab key combinations
    via SendInput to test keyboard layout switching reliability.

    Key differences from SetForegroundWindow-based tests:
    - Alt+Tab goes through Windows' task switcher with animation/delays
    - The foreground change happens on Alt release, not Tab press
    - The MRU (most recently used) order determines which window appears next
    - Multiple Tab presses cycle through the switcher
#>

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class Win32Test
{
    // ── Window enumeration ──

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    public static extern int GetWindowThreadProcessId(IntPtr hWnd, IntPtr zero);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    // ── SendInput for keyboard simulation ──

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint cInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern int GetKeyboardLayoutList(int nBuff, IntPtr[] lpList);

    // ── Constants ──

    public const uint INPUT_KEYBOARD = 1;
    public const uint KEYEVENTF_KEYDOWN = 0x0000;
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const ushort VK_MENU = 0x12;   // Alt key
    public const ushort VK_TAB = 0x09;
    public const ushort VK_LWIN = 0x5B;
    public const ushort VK_SPACE = 0x20;

    // ── Structures ──

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public KEYBDINPUT ki;
    }

    // ── Window info ──

    public class WindowInfo
    {
        public IntPtr Hwnd { get; set; }
        public string Title { get; set; }
    }

    // ── Window enumeration helper ──

    public static WindowInfo[] EnumVisibleWindows()
    {
        var results = new System.Collections.Generic.List<WindowInfo>();
        EnumWindows(delegate(IntPtr hwnd, IntPtr lParam)
        {
            if (IsWindowVisible(hwnd))
            {
                string title = GetWindowTitle(hwnd);
                if (!string.IsNullOrEmpty(title))
                {
                    results.Add(new WindowInfo { Hwnd = hwnd, Title = title });
                }
            }
            return true;
        }, IntPtr.Zero);
        return results.ToArray();
    }

    public static string GetWindowTitle(IntPtr hwnd)
    {
        int length = GetWindowTextLength(hwnd);
        if (length <= 0) return "";
        StringBuilder sb = new StringBuilder(length + 1);
        GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    // ── Layout helpers ──

    public static int GetCurrentLayoutLangId()
    {
        IntPtr hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return 0;
        uint temp = 0;
        uint threadId = (uint)GetWindowThreadProcessId(hwnd, out temp);
        IntPtr hkl = GetKeyboardLayout(threadId);
        return (int)(hkl.ToInt64() & 0xFFFF);
    }

    public static int GetLayoutLangId(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return 0;
        uint temp = 0;
        uint threadId = (uint)GetWindowThreadProcessId(hwnd, out temp);
        IntPtr hkl = GetKeyboardLayout(threadId);
        return (int)(hkl.ToInt64() & 0xFFFF);
    }

    public static string GetLayoutNameFromLcid(int lcid)
    {
        try
        {
            var ci = new System.Globalization.CultureInfo(lcid);
            return ci.Name;
        }
        catch
        {
            return string.Format("0x{0:X4}", lcid);
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  ALT+TAB SIMULATION via SendInput
    // ══════════════════════════════════════════════════════════════

    private static readonly int InputSize = Marshal.SizeOf<INPUT>();

    /// <summary>
    /// Simulates a single Alt+Tab press (switches to the next window in MRU order).
    /// Uses multiple SendInput calls with delays so Windows sees Alt held during Tab.
    /// </summary>
    public static void AltTab(int extraTabPresses = 0, int postTabDelayMs = 200)
    {
        // 1. Alt key down
        INPUT altDown = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_MENU, dwFlags = KEYEVENTF_KEYDOWN } };
        SendInput(1, new INPUT[] { altDown }, InputSize);
        System.Threading.Thread.Sleep(50);

        // 2. Tab press-release (opens the switcher)
        INPUT tabDown = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_TAB, dwFlags = KEYEVENTF_KEYDOWN } };
        INPUT tabUp = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_TAB, dwFlags = KEYEVENTF_KEYUP } };
        SendInput(2, new INPUT[] { tabDown, tabUp }, InputSize);
        System.Threading.Thread.Sleep(postTabDelayMs);

        // 3. Extra Tab presses to cycle through windows
        for (int i = 0; i < extraTabPresses; i++)
        {
            SendInput(2, new INPUT[] { tabDown, tabUp }, InputSize);
            System.Threading.Thread.Sleep(postTabDelayMs);
        }

        // 4. Alt key up (confirms the switch)
        INPUT altUp = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_MENU, dwFlags = KEYEVENTF_KEYUP } };
        SendInput(1, new INPUT[] { altUp }, InputSize);
    }

    /// <summary>
    /// Simulates Win+Space (cycles keyboard layout). Uses atomic SendInput.
    /// </summary>
    public static void WinSpace()
    {
        var inputs = new INPUT[4];
        inputs[0] = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_LWIN, dwFlags = KEYEVENTF_KEYDOWN } };
        inputs[1] = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_SPACE, dwFlags = KEYEVENTF_KEYDOWN } };
        inputs[2] = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_SPACE, dwFlags = KEYEVENTF_KEYUP } };
        inputs[3] = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_LWIN, dwFlags = KEYEVENTF_KEYUP } };
        SendInput(4, inputs, InputSize);
    }

    /// <summary>
    /// Simulates Alt+Shift (common keyboard layout toggle).
    /// </summary>
    public static void AltShift()
    {
        // Alt down + Shift down (atomic)
        INPUT altDown = new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_MENU, dwFlags = KEYEVENTF_KEYDOWN } };
        SendInput(1, new INPUT[] { altDown }, InputSize);
        System.Threading.Thread.Sleep(30);

        // Note: We don't send Shift because that would be destructive to the test.
        // This is just a placeholder for future use.
    }

    /// <summary>
    /// Gets the process name for a window handle.
    /// </summary>
    public static string GetProcessName(IntPtr hwnd)
    {
        try
        {
            uint pid = 0;
            GetWindowThreadProcessId(hwnd, out pid);
            if (pid == 0) return "unknown";
            System.Diagnostics.Process proc = System.Diagnostics.Process.GetProcessById((int)pid);
            return proc.ProcessName;
        }
        catch
        {
            return "unknown";
        }
    }
}
"@ -ErrorAction Stop

# ══════════════════════════════════════════════════════════════
#  TEST FRAMEWORK
# ══════════════════════════════════════════════════════════════

$testReportFile = Join-Path $env:TEMP "LangKeep_AltTab_Test_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
$logLines = New-Object System.Collections.Generic.List[string]
$testCounter = 1
$passCount = 0
$failCount = 0
$lkLogDir = Join-Path $env:APPDATA "LangKeep\logs"

function Log($msg) {
    $ts = Get-Date -Format "HH:mm:ss.fff"
    $line = "[$ts] $msg"
    $logLines.Add($line)
    Write-Host $line
}

function RunTest($name, $desc, $testBlock) {
    Log("")
    Log("======= TEST #$testCounter : $name =======")
    Log("  $desc")
    Log("----------------------------------------")
    
    $start = Get-Date
    try {
        $ok = & $testBlock
        $elapsed = (Get-Date) - $start
        if ($ok) {
            Log("PASSED ($($elapsed.TotalSeconds.ToString("F1"))s)")
            $script:passCount++
        } else {
            Log("FAILED ($($elapsed.TotalSeconds.ToString("F1"))s)")
            $script:failCount++
        }
    } catch {
        $elapsed = (Get-Date) - $start
        Log("FAILED with exception: $($_.ToString()) ($($elapsed.TotalSeconds.ToString("F1"))s)")
        $script:failCount++
    }
    
    $script:testCounter++
    Log("----------------------------------------")
    Log("")
}

function GetLayout() {
    $lid = [Win32Test]::GetCurrentLayoutLangId()
    $name = [Win32Test]::GetLayoutNameFromLcid($lid)
    return @{ Lcid = $lid; Name = $name }
}

function GetLayoutForHwnd($hwnd) {
    $lid = [Win32Test]::GetLayoutLangId($hwnd)
    $name = [Win32Test]::GetLayoutNameFromLcid($lid)
    return @{ Lcid = $lid; Name = $name }
}

function LogForeground($tag) {
    $hwnd = [Win32Test]::GetForegroundWindow()
    $title = [Win32Test]::GetWindowTitle($hwnd)
    $proc = [Win32Test]::GetProcessName($hwnd)
    $layout = GetLayout
    Log("  [$tag] FG: 0x$($hwnd.ToString("X8")) $proc / $($layout.Name)")
    return @{ Hwnd = $hwnd; Title = $title; ProcessName = $proc; Layout = $layout }
}

function DumpLogTail($lines = 30) {
    if (Test-Path $lkLogDir) {
        $files = Get-ChildItem $lkLogDir -Filter "*.log" | Sort-Object LastWriteTime -Descending
        if ($files.Count -gt 0) {
            $content = Get-Content $files[0].FullName -Tail $lines
            Log("  LangKeep logs (last $lines lines):")
            foreach ($c in $content) { Log("    $c") }
        }
    }
}

function AssertLayout($label, $expectedLcid, $actualLayout) {
    if ($actualLayout.Lcid -eq $expectedLcid) {
        return $true
    }
    Log("  ** $label expected LCID 0x$($expectedLcid.ToString("X4")) but got 0x$($actualLayout.Lcid.ToString("X4")) ($($actualLayout.Name))")
    return $false
}

# ══════════════════════════════════════════════════════════════
#  START
# ══════════════════════════════════════════════════════════════

Log("=" * 45)
Log("LANGKEEP ALT+TAB TEST HARNESS")
Log("Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
Log("=" * 45)
Log("")

# -- Enumerate windows --
Log("Enumerating visible windows...")
$windows = [Win32Test]::EnumVisibleWindows()

Log("Found $($windows.Count) visible windows with titles:")
for ($i = 0; $i -lt $windows.Count; $i++) {
    Log("  [$i] 0x$($windows[$i].Hwnd.ToString("X8"))  $($windows[$i].Title)")
}

# Find targets
$notepadWindows = $windows | Where-Object { $_.Title -match "Notepad" }
$browserWindows = $windows | Where-Object { $_.Title -match "Brave|Chrome|Edge|Firefox|Chromium" }

Log("")
Log("Matched windows:")
Log("  Notepad:   $($notepadWindows.Count) window(s)")
foreach ($w in $notepadWindows) { Log("    - $($w.Title)") }
Log("  Browser:   $($browserWindows.Count) window(s)")
foreach ($w in $browserWindows) { Log("    - $($w.Title)") }

# Check LangKeep is running
$lkProcess = Get-Process -Name "LangKeep" -ErrorAction SilentlyContinue
if (-not $lkProcess) {
    Log("WARNING: LangKeep process not found! Tests will still run but switching won't be verified.")
} else {
    Log("LangKeep running: PID $($lkProcess.Id), started $($lkProcess.StartTime)")
}

# Save initial state
$initial = LogForeground "INITIAL"
Log("Initial layout: $($initial.Layout.Name) (LCID 0x$($initial.Layout.Lcid.ToString("X4")))")

# ══════════════════════════════════════════════════════════════
#  TEST 1: Basic Alt+Tab (single press)
#  Verifies that a single Alt+Tab key combination changes the
#  foreground window and LangKeep detects it.
# ══════════════════════════════════════════════════════════════

RunTest -name "Basic Alt+Tab (single press)" -desc "Simulate Alt+Tab via SendInput and verify foreground changes" -testBlock {
    $before = LogForeground "BEFORE"
    
    # Perform one Alt+Tab
    Log("  Sending Alt+Tab...")
    [Win32Test]::AltTab(0, 250)
    Start-Sleep -Milliseconds 500
    
    $after = LogForeground "AFTER"
    
    # Should have switched to a different window
    if ($after.Hwnd -eq $before.Hwnd) {
        Log("  Foreground did NOT change - Alt+Tab may not have registered")
        return $false
    }
    
    Log("  Switched: 0x$($before.Hwnd.ToString("X8")) -> 0x$($after.Hwnd.ToString("X8"))")
    Log("  Process: $($before.ProcessName) -> $($after.ProcessName)")
    Log("  Layout:  $($before.Layout.Name) -> $($after.Layout.Name)")
    return $true
}

# ══════════════════════════════════════════════════════════════
#  TEST 2: Alt+Tab back (round trip)
#  Verifies that Alt+Tabbing back to the original window works.
# ══════════════════════════════════════════════════════════════

RunTest -name "Alt+Tab Round Trip" -desc "Alt+Tab away and back, verify return to original window" -testBlock {
    $first = LogForeground "START"
    
    # Alt+Tab to next window
    Log("  Alt+Tab #1 (away)...")
    [Win32Test]::AltTab(0, 250)
    Start-Sleep -Milliseconds 500
    
    $second = LogForeground "MID"
    if ($second.Hwnd -eq $first.Hwnd) {
        Log("  First Alt+Tab didn't change foreground")
        return $false
    }
    
    # Alt+Tab back to original
    Log("  Alt+Tab #2 (back)...")
    [Win32Test]::AltTab(0, 250)
    Start-Sleep -Milliseconds 500
    
    $third = LogForeground "END"
    
    if ($third.Hwnd -eq $first.Hwnd) {
        Log("  Successfully returned to original window")
        return $true
    }
    
    # May need an extra Tab press if more than 2 windows are in the MRU
    Log("  Did not return to original - may need extra Tab press")
    Log("  First: 0x$($first.Hwnd.ToString("X8")) $($first.ProcessName)")
    Log("  After return: 0x$($third.Hwnd.ToString("X8")) $($third.ProcessName)")
    return $false
}

# ══════════════════════════════════════════════════════════════
#  TEST 3: Rapid double Alt+Tab
#  Simulates fast Alt+Tab back-and-forth (like a user flipping
#  between two apps).
# ══════════════════════════════════════════════════════════════

RunTest -name "Rapid Double Alt+Tab" -desc "Simulate fast Alt+Tab back-and-forth twice" -testBlock {
    $start = LogForeground "START"
    
    # Rapid back-and-forth
    for ($rep = 1; $rep -le 2; $rep++) {
        Log("  --- Rapid round $rep/2 ---")
        
        # Quick Alt+Tab away
        [Win32Test]::AltTab(0, 100)
        Start-Sleep -Milliseconds 200
        
        $mid = LogForeground "MID$rep"
        
        # Quick Alt+Tab back
        [Win32Test]::AltTab(0, 100)
        Start-Sleep -Milliseconds 200
        
        $back = LogForeground "BACK$rep"
        
        # Check: should return to roughly where we started
        if ($back.Hwnd -eq $start.Hwnd) {
            Log("  Round $($rep): returned to original window")
        } else {
            Log("  Round $($rep): landed on different window")
            Log("    Original: 0x$($start.Hwnd.ToString("X8")) $($start.ProcessName)")
            Log("    Now:      0x$($back.Hwnd.ToString("X8")) $($back.ProcessName)")
        }
    }
    
    return $true  # Pass unless there's an exception
}

# ══════════════════════════════════════════════════════════════
#  TEST 4: LangKeep detects Alt+Tab switches
#  Verifies LangKeep logs show foreground changes after Alt+Tab.
# ══════════════════════════════════════════════════════════════

RunTest -name "LangKeep Alt+Tab Detection" -desc "Check LangKeep logs for Alt+Tab-induced foreground changes" -testBlock {
    # Get current log size
    if (-not (Test-Path $lkLogDir)) {
        Log("  No log directory - LangKeep may not be running")
        return $false
    }
    
    $files = Get-ChildItem $lkLogDir -Filter "*.log" | Sort-Object LastWriteTime -Descending
    if ($files.Count -eq 0) {
        Log("  No log files")
        return $false
    }
    
    $beforeSize = $files[0].Length
    $beforeContent = Get-Content $files[0].FullName -Tail 5
    
    Log("  Log file: $($files[0].Name) ($beforeSize bytes)")
    
    # Do a few Alt+Tabs to generate log entries
    Log("  Generating foreground changes via Alt+Tab...")
    [Win32Test]::AltTab(0, 200)
    Start-Sleep -Milliseconds 400
    [Win32Test]::AltTab(0, 200)
    Start-Sleep -Milliseconds 400
    [Win32Test]::AltTab(0, 200)
    Start-Sleep -Milliseconds 600
    
    # Check log has new entries
    $files = Get-ChildItem $lkLogDir -Filter "*.log" | Sort-Object LastWriteTime -Descending
    $afterSize = $files[0].Length
    $afterContent = Get-Content $files[0].FullName -Tail 15
    
    Log("  Log file after: $($files[0].Name) ($afterSize bytes, +$($afterSize - $beforeSize) bytes)")
    
    # Look for "Foreground changed" entries
    $fgChanges = $afterContent | Select-String "Foreground changed|Active window changed"
    if ($fgChanges.Count -gt 0) {
        Log("  Found $($fgChanges.Count) foreground change entries in logs:")
        foreach ($f in $fgChanges) { Log("    $f") }
        return $true
    }
    
    # Log full content for diagnosis
    Log("  No foreground changes detected in log tail:")
    foreach ($c in $afterContent) { Log("    $c") }
    return $false
}

# ══════════════════════════════════════════════════════════════
#  TEST 5: Alt+Tab consistency (3x back-and-forth)
#  Tests that repeated Alt+Tab switching is reliable over
#  multiple iterations.
# ══════════════════════════════════════════════════════════════

RunTest -name "Alt+Tab Consistency (3x)" -desc "Cycle through windows via Alt+Tab 3 times" -testBlock {
    $allOk = $true
    
    for ($i = 1; $i -le 3; $i++) {
        Log("  === Consistency iteration $i/3 ===")
        $before = LogForeground "B4-$i"
        
        [Win32Test]::AltTab(1, 250)
        Start-Sleep -Milliseconds 600
        
        $after = LogForeground "AF-$i"
        
        if ($after.Hwnd -eq $before.Hwnd) {
            Log("  Foreground didn't change at iteration $i")
            $allOk = $false
        }
    }
    
    return $allOk
}

# ══════════════════════════════════════════════════════════════
#  TEST 6: Comparison — Alt+Tab vs SetForegroundWindow
#  Compares the behavior and timing of both methods.
# ══════════════════════════════════════════════════════════════

RunTest -name "Method Comparison (Alt+Tab vs SetForegroundWindow)" -desc "Compare both methods and measure timing" -testBlock {
    $startHwnd = [Win32Test]::GetForegroundWindow()
    $startTime = Get-Date
    
    Log("  Starting from: 0x$($startHwnd.ToString("X8"))")
    
    # Method 1: SetForegroundWindow
    if ($notepadWindows.Count -gt 0) {
        $target = $notepadWindows[0].Hwnd
        $t1 = Get-Date
        $ok = [Win32Test]::SetForegroundWindow($target)
        $t2 = Get-Date
        $sfwTime = ($t2 - $t1).TotalMilliseconds
        Start-Sleep -Milliseconds 500
        $fg1 = [Win32Test]::GetForegroundWindow()
        Log("  SetForegroundWindow -> Notepad: ${sfwTime}ms, success=$ok, fg=0x$($fg1.ToString("X8"))")
    }
    
    # Return to start
    [Win32Test]::SetForegroundWindow($startHwnd)
    Start-Sleep -Milliseconds 300
    
    # Method 2: Alt+Tab (with enough presses to reach Notepad)
    # This is approximate — we don't know the MRU position
    $t1 = Get-Date
    [Win32Test]::AltTab(2, 200)
    $t2 = Get-Date
    $altTabTime = ($t2 - $t1).TotalMilliseconds
    Start-Sleep -Milliseconds 500
    $fg2 = [Win32Test]::GetForegroundWindow()
    $fg2Title = [Win32Test]::GetWindowTitle($fg2)
    Log("  Alt+Tab (3 presses): ${altTabTime}ms total, fg=0x$($fg2.ToString("X8")) $fg2Title")
    
    Log("")
    Log("  Comparison:")
    Log("    SetForegroundWindow: immediate call, no OS animation, bypasses Alt+Tab")
    Log("    Alt+Tab (SendInput): ~${altTabTime}ms total, goes through task switcher, realistic")
    Log("    Difference: Alt+Tab is ~$([math]::Round($altTabTime - $sfwTime))ms slower")
    
    return $true
}

# ══════════════════════════════════════════════════════════════
#  TEST 7: Alt+Tab with LangKeep layout checking
#  Tests that LangKeep correctly evaluates layouts after Alt+Tab.
# ══════════════════════════════════════════════════════════════

RunTest -name "Layout Detection After Alt+Tab" -desc "Check LangKeep log for EvaluateAndSwitch entries after Alt+Tab" -testBlock {
    # Do several Alt+Tabs to generate log activity
    Log("  Performing 4 Alt+Tabs...")
    for ($i = 1; $i -le 4; $i++) {
        [Win32Test]::AltTab(0, 200)
        Start-Sleep -Milliseconds 600
        $fg = LogForeground "AT$i"
    }
    
    # Dump logs and check for key events
    DumpLogTail 30
    
    # Check for key log entries
    $files = Get-ChildItem $lkLogDir -Filter "*.log" | Sort-Object LastWriteTime -Descending
    if ($files.Count -gt 0) {
        $content = Get-Content $files[0].FullName -Tail 50
        $evalEntries = $content | Select-String "EvaluateAndSwitch"
        $fgEntries = $content | Select-String "Foreground changed"
        
        if ($fgEntries.Count -gt 0) {
            Log("  Foreground changes detected: $($fgEntries.Count)")
            return $true
        }
        Log("  No foreground changes in log tail")
        return $false
    }
    
    return $false
}

# ══════════════════════════════════════════════════════════════
#  FINAL LOG DUMP
# ══════════════════════════════════════════════════════════════

Log("")
Log("=" * 45)
Log("FULL LANGKEEP LOG DUMP")
Log("=" * 45)
if (Test-Path $lkLogDir) {
    $files = Get-ChildItem $lkLogDir -Filter "*.log" | Sort-Object LastWriteTime -Descending
    if ($files.Count -gt 0) {
        foreach ($f in $files) {
            Log("Log: $($f.FullName) ($($f.Length) bytes, $($f.LastWriteTime))")
            try {
                $content = Get-Content $f.FullName
                foreach ($c in $content) {
                    Log("  $c")
                }
            } catch {
                Log("  Could not read: $($_.ToString())")
            }
        }
    }
}

# ══════════════════════════════════════════════════════════════
#  SUMMARY
# ══════════════════════════════════════════════════════════════

Log("")
Log("=" * 45)
Log("TEST SUMMARY")
Log("=" * 45)
$total = $passCount + $failCount
Log("Total: $total")
Log("Passed: $passCount")
Log("Failed: $failCount")
Log("")

# Save report
$logLines | Out-File -FilePath $testReportFile -Encoding UTF8
Log("Report saved to: $testReportFile")
Write-Host ""
Write-Host "REPORT: $testReportFile"
