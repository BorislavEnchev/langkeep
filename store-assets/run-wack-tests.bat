@echo off
title LangKeep — Windows App Certification Kit (WACK)
cd /d "%~dp0"

set MSIX_PATH=C:\Users\kiborko\desktop\github\langkeep\artifacts\LangKeep-0.2.2.0-x64.msix
set REPORT_PATH=C:\Users\kiborko\desktop\github\langkeep\artifacts\wack-report.xml

echo =============================================
echo  LangKeep — WACK Test Suite
echo =============================================
echo.
echo MSIX:  %MSIX_PATH%
echo Report: %REPORT_PATH%
echo.
echo This script MUST be run as Administrator.
echo Right-click this file and select "Run as administrator".
echo.
echo =============================================
echo.

:: Check if running as admin
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo Please right-click this file and select "Run as administrator".
    pause
    exit /b 1
)

echo [1/3] Resetting WACK...
"C:\Program Files (x86)\Windows Kits\10\App Certification Kit\appcert.exe" reset
if %errorlevel% neq 0 (
    echo WARNING: Reset may have failed, continuing anyway...
)

echo.
echo [2/3] Running WACK tests on MSIX package...
echo This may take several minutes. Please wait...
echo.
"C:\Program Files (x86)\Windows Kits\10\App Certification Kit\appcert.exe" test -appxpackagepath "%MSIX_PATH%" -reportoutputpath "%REPORT_PATH%"
set WACK_EXIT=%errorlevel%

echo.
echo =============================================
if %WACK_EXIT% equ 0 (
    echo RESULT: WACK tests completed successfully!
) else (
    echo RESULT: WACK tests completed with exit code %WACK_EXIT%
    echo Please check the report for details.
)
echo =============================================

:: Check if report was generated
if exist "%REPORT_PATH%" (
    echo.
    echo Report generated at: %REPORT_PATH%
    echo Opening report location...
    start "" "%REPORT_PATH%"
    
    :: Also try to open the HTML report if it exists
    if exist "%REPORT_PATH%.htm" (
        start "" "%REPORT_PATH%.htm"
    )
) else (
    echo.
    echo WARNING: Report file was not found at %REPORT_PATH%
    echo Check C:\Users\%USERNAME%\AppData\Local\Microsoft\Windows App Certification Kit\ for reports
)

echo.
pause
