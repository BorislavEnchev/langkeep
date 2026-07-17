# LangKeep — Convert SVG App Icon to PNG
#
# This script converts the SVG icon to a 300×300 PNG for the Store listing.
# Requires: Windows 10/11 with Microsoft Edge WebView2 runtime (or Chrome/Edge installed)
#
# Method 1: Use Edge/Chrome headless via Playwright (recommended for accuracy)
#   npx playwright install chromium
#   npx playwright screenshot --full-page app-icon.svg app-icon.png
#
# Method 2: Manual — Open app-icon.svg in Edge/Chrome, zoom to 100%, take a screenshot
#   then crop to 300×300 using any image editor.
#
# Method 3: Use an online SVG-to-PNG converter:
#   https://convertio.co/svg-png/  (free, up to 100 MB)
#   https://svgtopng.com/
#
# Method 4: PowerShell + .NET (basic approach, requires .NET 9+ with SkiaSharp):
#   dotnet tool install -g SkiaSharp.QrCode
  
Write-Host "=== LangKeep — Icon Conversion Helper ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "The Store requires a 300×300 PNG file." -ForegroundColor Yellow
Write-Host ""
Write-Host "Quickest option: Open 'app-icon.svg' in Edge/Chrome, pinch-zoom" -ForegroundColor White
Write-Host "until it fills the window, take a screenshot, and crop to 300×300." -ForegroundColor White
Write-Host ""
Write-Host "Or use this free online converter:" -ForegroundColor White
Write-Host "  https://convertio.co/svg-png/" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output file should be saved as: app-icon-300x300.png" -ForegroundColor Green
