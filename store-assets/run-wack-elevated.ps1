# LangKeep — Run WACK Tests on MSIX Package
# This script will prompt for UAC elevation.

$MSIX = "C:\Users\kiborko\desktop\github\langkeep\artifacts\LangKeep-0.2.2.0-x64.msix"
$Report = "C:\Users\kiborko\desktop\github\langkeep\artifacts\wack-report.xml"
$WackExe = "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\appcert.exe"

# Check if running as admin
$isAdmin = ([System.Security.Principal.WindowsIdentity]::GetCurrent().Groups -contains 'S-1-5-32-544')
if (-not $isAdmin) {
    Write-Host "Elevating to Administrator..." -ForegroundColor Yellow
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    Start-Process powershell.exe -Verb RunAs -ArgumentList $arguments
    exit
}

Write-Host "=== LangKeep — WACK Test Suite ===" -ForegroundColor Cyan
Write-Host "MSIX:  $MSIX"
Write-Host "Report: $Report"
Write-Host ""

# Reset WACK first
Write-Host "[1/2] Resetting WACK..." -ForegroundColor Yellow
& $WackExe reset
Write-Host ""

# Run the test
Write-Host "[2/2] Running WACK tests on MSIX package..." -ForegroundColor Yellow
Write-Host "This may take several minutes. Please wait..." -ForegroundColor Gray
Write-Host ""
& $WackExe test -appxpackagepath $MSIX -reportoutputpath $Report
$exitCode = $LASTEXITCODE

Write-Host ""
if ($exitCode -eq 0) {
    Write-Host "RESULT: WACK tests completed successfully! ✅" -ForegroundColor Green
} else {
    Write-Host "RESULT: WACK tests completed with exit code $exitCode" -ForegroundColor Yellow
    Write-Host "Please check the report for details." -ForegroundColor Yellow
}

# Check if reports were generated
if (Test-Path $Report) {
    $reportFile = Get-Item $Report
    Write-Host "Report: $($reportFile.FullName) ($($reportFile.Length) bytes)" -ForegroundColor Green
    Invoke-Item $Report
}

# Check for HTML report
$htmlReport = "$Report.htm"
if (Test-Path $htmlReport) {
    Write-Host "HTML Report: $htmlReport" -ForegroundColor Green
    Invoke-Item $htmlReport
}

# Also check default WACK output location
$defaultDir = "$env:LOCALAPPDATA\Microsoft\Windows App Certification Kit"
if (Test-Path $defaultDir) {
    Write-Host "Reports also available at: $defaultDir" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
