<#
.SYNOPSIS
    LangKeep Auto-Switching Test Harness - tests keyboard layout switching on Alt+Tab
#>

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class Win32Test
{
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    public static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    public class WindowInfo
    {
        public IntPtr Hwnd { get; set; }
        public string Title { get; set; }
    }

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

    public static int GetCurrentLayoutLangId()
    {
        IntPtr hwnd = GetForegroundWindow();
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
            string result = string.Format("0x{0:X4}", lcid);
            return result;
        }
    }
}
"@ -ErrorAction Stop

# Variables
$testReportFile = Join-Path $env:TEMP "LangKeep_Test_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
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
    Log("======= TEST #" + $testCounter + " : " + $name + " =======")
    Log("  " + $desc)
    Log("----------------------------------------")
    
    $start = Get-Date
    try {
        $ok = & $testBlock
        $elapsed = (Get-Date) - $start
        if ($ok) {
            Log("PASSED (" + $elapsed.TotalSeconds.ToString("F1") + "s)")
            $script:passCount++
        } else {
            Log("FAILED (" + $elapsed.TotalSeconds.ToString("F1") + "s)")
            $script:failCount++
        }
    } catch {
        $elapsed = (Get-Date) - $start
        Log("FAILED with exception: " + $_.ToString() + " (" + $elapsed.TotalSeconds.ToString("F1") + "s)")
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

function SwitchTo($hwnd, $label) {
    $before = GetLayout
    Log("  Switch to: " + $label + " (HWND 0x" + $hwnd.ToString("X8") + ")")
    Log("  Before: " + $before.Name + " (0x" + $before.Lcid.ToString("X4") + ")")
    
    $ok = [Win32Test]::SetForegroundWindow($hwnd)
    if (-not $ok) {
        Log("  SetForegroundWindow FAILED")
        return $null
    }
    Start-Sleep -Milliseconds 1000
    
    $after = GetLayout
    Log("  After:  " + $after.Name + " (0x" + $after.Lcid.ToString("X4") + ")")
    
    if ($before.Lcid -ne $after.Lcid) {
        Log("  Layout changed: " + $before.Name + " -> " + $after.Name)
    } else {
        Log("  Layout unchanged: " + $before.Name)
    }
    return $after
}

function CheckLogs($tag) {
    Start-Sleep -Milliseconds 1500
    if (Test-Path $lkLogDir) {
        $files = Get-ChildItem $lkLogDir -Filter "*.log" | Sort-Object LastWriteTime -Descending
        if ($files.Count -gt 0) {
            $lines = Get-Content $files[0].FullName -Tail 30
            Log("  LangKeep logs [" + $tag + "]:")
            foreach ($l in $lines) {
                Log("    $l")
            }
        } else {
            Log("  No log files found yet")
        }
    } else {
        Log("  Log dir does not exist: $lkLogDir")
    }
}

# === START ===
Log("==========================================")
Log("LANGKEEP AUTO-SWITCHING TEST HARNESS")
Log("Started: " + (Get-Date -Format "yyyy-MM-dd HH:mm:ss"))
Log("==========================================")
Log("")

# Enumerate visible windows using a native helper inside the Add-Type block
Log("Enumerating visible windows...")
$windows = [Win32Test]::EnumVisibleWindows()

Log("Found " + $windows.Count + " visible windows with titles:")
for ($i = 0; $i -lt $windows.Count; $i++) {
    Log("  [" + $i + "] 0x" + $windows[$i].Hwnd.ToString("X8") + "  " + $windows[$i].Title)
}

# Find target windows
$notepadWindows = $windows | Where-Object { $_.Title -match "Notepad" }
$browserWindows = $windows | Where-Object { $_.Title -match "Brave|Chrome|Edge|Firefox|Chromium" }
$terminalWindows = $windows | Where-Object { $_.Title -match "Terminal|PowerShell|cmd|MINGW|Git Bash|Windows PowerShell" }

Log("")
Log("Matched windows:")
Log("  Notepad:   " + $notepadWindows.Count + " window(s)")
foreach ($w in $notepadWindows) { Log("    - " + $w.Title) }
Log("  Browser:   " + $browserWindows.Count + " window(s)")
foreach ($w in $browserWindows) { Log("    - " + $w.Title) }
Log("  Terminal:  " + $terminalWindows.Count + " window(s)")
foreach ($w in $terminalWindows) { Log("    - " + $w.Title) }

# Save initial foreground
$initialHwnd = [Win32Test]::GetForegroundWindow()
$initialTitle = [Win32Test]::GetWindowTitle($initialHwnd)
Log("")
Log("Initial foreground: 0x" + $initialHwnd.ToString("X8") + "  " + $initialTitle)
$initialLayout = GetLayout
Log("Initial layout: " + $initialLayout.Name + " (LCID 0x" + $initialLayout.Lcid.ToString("X4") + ")")

# === TEST 1: Window Enumeration ===
RunTest -name "Window Enumeration" -desc "Check that we can find Notepad and Browser windows" -testBlock {
    $foundNp = $notepadWindows.Count -gt 0
    $foundBr = $browserWindows.Count -gt 0
    Log("  Notepad found: " + $foundNp)
    Log("  Browser found: " + $foundBr)
    return $foundNp -and $foundBr
}

# === TEST 2: Initial LangKeep Log State ===
RunTest -name "LangKeep Logging Active" -desc "Check that LangKeep is writing logs to disk" -testBlock {
    if (Test-Path $lkLogDir) {
        $files = Get-ChildItem $lkLogDir -Filter "*.log" | Sort-Object LastWriteTime -Descending
        if ($files.Count -gt 0) {
            $content = Get-Content $files[0].FullName -Tail 10
            Log("Found " + $files.Count + " log file(s)")
            Log("Latest: " + $files[0].Name + " (" + $files[0].Length + " bytes)")
            foreach ($c in $content) { Log("  " + $c) }
            return $true
        }
        Log("No log files yet")
        # Check if we can write a test file
        $testFile = Join-Path $lkLogDir "test_write.txt"
        try { Set-Content -Path $testFile -Value "test" -ErrorAction Stop; Remove-Item $testFile; Log("Directory is writable") } catch { Log("Cannot write: " + $_.ToString()) }
        return $false
    }
    Log("No log dir: " + $lkLogDir)
    return $false
}

# === TEST 3: Switch to Browser (expect en-US) ===
if ($browserWindows.Count -gt 0) {
    RunTest -name "Browser switch (en-US)" -desc "Preferences: brave.exe -> en-US. Switch to browser." -testBlock {
        $layout = SwitchTo $browserWindows[0].Hwnd $browserWindows[0].Title
        CheckLogs "browser"
        return $layout -ne $null -and ($layout.Lcid -eq 0x0409 -or $layout.Name -eq "en-US")
    }
}

# === TEST 4: Switch to Notepad (expect en-US) ===
if ($notepadWindows.Count -gt 0) {
    RunTest -name "Notepad switch (en-US)" -desc "Preferences: Notepad.exe -> en-US. Switch to Notepad." -testBlock {
        $layout = SwitchTo $notepadWindows[0].Hwnd $notepadWindows[0].Title
        CheckLogs "notepad"
        return $layout -ne $null -and ($layout.Lcid -eq 0x0409 -or $layout.Name -eq "en-US")
    }
}

# === TEST 5: Rapid double switch ===
if ($browserWindows.Count -gt 0 -and $notepadWindows.Count -gt 0) {
    RunTest -name "Rapid double switch" -desc "Switch Browser -> Notepad, then Notepad -> Browser quickly" -testBlock {
        Log("  Step 1: Browser -> Notepad")
        $layout1 = SwitchTo $notepadWindows[0].Hwnd $notepadWindows[0].Title
        CheckLogs "rapid1"
        Start-Sleep -Milliseconds 200
        
        Log("  Step 2: Notepad -> Browser")
        $layout2 = SwitchTo $browserWindows[0].Hwnd $browserWindows[0].Title
        CheckLogs "rapid2"
        
        $ok1 = $layout1 -ne $null -and ($layout1.Lcid -eq 0x0409 -or $layout1.Name -eq "en-US")
        $ok2 = $layout2 -ne $null -and ($layout2.Lcid -eq 0x0409 -or $layout2.Name -eq "en-US")
        Log("  Notepad correct: " + $ok1)
        Log("  Browser correct: " + $ok2)
        return $ok1 -and $ok2
    }
}

# === TEST 6: Consistency test - 3 back-and-forth ===
if ($browserWindows.Count -gt 0 -and $notepadWindows.Count -gt 0) {
    RunTest -name "Consistency 3x" -desc "Switch Browser->Notepad->Browser 3 times, check consistency" -testBlock {
        $allOk = $true
        for ($i = 1; $i -le 3; $i++) {
            Log("  === Iteration " + $i + "/3: Notepad -> Browser ===")
            $layout = SwitchTo $browserWindows[0].Hwnd $browserWindows[0].Title
            if ($layout -eq $null -or ($layout.Lcid -ne 0x0409 -and $layout.Name -ne "en-US")) {
                Log("  FAILED at browser iteration " + $i)
                $allOk = $false
            }
            CheckLogs ("consistency-browser-" + $i)
            Start-Sleep -Milliseconds 500
            
            Log("  === Iteration " + $i + "/3: Browser -> Notepad ===")
            $layout = SwitchTo $notepadWindows[0].Hwnd $notepadWindows[0].Title
            if ($layout -eq $null -or ($layout.Lcid -ne 0x0409 -and $layout.Name -ne "en-US")) {
                Log("  FAILED at notepad iteration " + $i)
                $allOk = $false
            }
            CheckLogs ("consistency-notepad-" + $i)
            Start-Sleep -Milliseconds 500
        }
        Log("Consistency result: " + $allOk)
        return $allOk
    }
}

# === TEST 7: Final LangKeep Log Dump ===
Log("")
Log("==========================================")
Log("FULL LANGKEEP LOG DUMP")
Log("==========================================")
if (Test-Path $lkLogDir) {
    $files = Get-ChildItem $lkLogDir -Filter "*.log" | Sort-Object LastWriteTime -Descending
    if ($files.Count -gt 0) {
        foreach ($f in $files) {
            Log("Log: " + $f.FullName + " (" + $f.Length + " bytes, " + $f.LastWriteTime + ")")
            try {
                $content = Get-Content $f.FullName
                foreach ($c in $content) {
                    Log("  $c")
                }
            } catch {
                Log("  Could not read: " + $_.ToString())
            }
        }
    } else {
        Log("No log files in directory")
    }
} else {
    Log("No log directory at: $lkLogDir")
    $baseDir = Join-Path $env:APPDATA "LangKeep"
    if (Test-Path $baseDir) {
        Log("Base directory exists:")
        Get-ChildItem $baseDir | ForEach-Object { Log("  " + $_.Name + " (" + $_.Length + " bytes, " + $_.LastWriteTime + ")") }
    }
}

# === SUMMARY ===
Log("")
Log("==========================================")
Log("TEST SUMMARY")
Log("==========================================")
$total = $passCount + $failCount
Log("Total: " + $total)
Log("Passed: " + $passCount)
Log("Failed: " + $failCount)
Log("")

# Save report
$logLines | Out-File -FilePath $testReportFile -Encoding UTF8
Log("Report saved to: " + $testReportFile)

# Show file path for easy access
Write-Host ""
Write-Host "REPORT: $testReportFile"
