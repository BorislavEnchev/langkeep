@echo off
title LangKeep - Final WACK Test
cd /d "%~dp0"

set "MSIX=C:\Users\kiborko\desktop\github\langkeep\artifacts\LangKeep-0.2.2.0-x64.msix"
set "REPORT=C:\Users\kiborko\desktop\github\langkeep\artifacts\wack-report.xml"
set "WACK=C:\Program Files (x86)\Windows Kits\10\App Certification Kit\appcert.exe"

echo =============================================
echo  LangKeep - Final WACK Certification Test
echo =============================================
echo.
echo MSIX:  %MSIX%
echo.
echo This script MUST be run as Administrator.
echo Right-click this file and select "Run as administrator".
echo.
echo =============================================
echo.

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Must run as Administrator!
    pause
    exit /b 1
)

echo [1/2] Running WACK certification tests...
echo This takes 2-5 minutes. Please wait...
echo.

"%WACK%" test -appxpackagepath "%MSIX%" -reportoutputpath "%REPORT%"
set WACK_EXIT=%errorlevel%

echo.
if %WACK_EXIT% equ 0 (
    echo RESULT: WACK completed with exit code 0
) else (
    echo RESULT: WACK completed with exit code %WACK_EXIT%
)

if exist "%REPORT%" (
    echo Report: %REPORT%
    start "" "%REPORT%"
)
if exist "%REPORT%.htm" (
    start "" "%REPORT%.htm"
)

echo.
pause
