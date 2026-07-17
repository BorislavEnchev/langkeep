# LangKeep — Generate MSIX Resource Icons
# Creates proper PNG icons that pass WACK certification

Add-Type -AssemblyName 'System.Drawing'

$outputDir = "C:\Users\kiborko\desktop\github\langkeep\src\LangKeep.Packaging\Resources"

# Helper to create a rounded rectangle path
function Add-RoundedRectPath {
    param([System.Drawing.Drawing2D.GraphicsPath]$path, [int]$x, [int]$y, [int]$w, [int]$h, [int]$r)
    $path.AddArc($x, $y, $r * 2, $r * 2, 180, 90)
    $path.AddArc($x + $w - $r * 2, $y, $r * 2, $r * 2, 270, 90)
    $path.AddArc($x + $w - $r * 2, $y + $h - $r * 2, $r * 2, $r * 2, 0, 90)
    $path.AddArc($x, $y + $h - $r * 2, $r * 2, $r * 2, 90, 90)
    $path.CloseFigure()
}

# Colors
$blue1 = [System.Drawing.Color]::FromArgb(0, 120, 212)    # #0078D4
$blue2 = [System.Drawing.Color]::FromArgb(16, 110, 190)   # #106EBE
$cyanLight = [System.Drawing.Color]::FromArgb(80, 230, 255) # #50E6FF
$darkBg = [System.Drawing.Color]::FromArgb(26, 26, 46)     # #1A1A2E
$white = [System.Drawing.Color]::White
$transparent = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)

# Draw a keyboard + globe icon (similar to the SVG design)
function New-LangKeepIcon {
    param([int]$size)

    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'HighQuality'
    $g.InterpolationMode = 'HighQualityBicubic'

    # Background: rounded square with gradient
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $corner = [Math]::Max(1, $size / 6)
    Add-RoundedRectPath -path $path -x 0 -y 0 -w $size -h $size -r $corner

    # Draw gradient background
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point(0, 0)),
        (New-Object System.Drawing.Point($size, $size)),
        $blue1, $blue2)
    $g.FillPath($brush, $path)
    $brush.Dispose()

    # Keyboard body (simplified at center-bottom area)
    $keyArea = $size * 0.5
    $keyTop = $size * 0.35
    $keyW = $size * 0.55
    $keyH = $size * 0.28

    $keyRect = New-Object System.Drawing.RectangleF(
        ($size - $keyW) / 2, $keyTop, $keyW, $keyH)

    $keyPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    Add-RoundedRectPath -path $keyPath -x $keyRect.X -y $keyRect.Y -w $keyRect.Width -h $keyRect.Height -r 4
    $keyBrush = New-Object System.Drawing.SolidBrush($darkBg)
    $g.FillPath($keyBrush, $keyPath)
    $keyBrush.Dispose()

    # Keyboard keys (simplified as dots)
    $keySize = $keyW / 8
    $keyGap = $keySize * 0.25
    $cols = 5
    $rows = 3
    $keyColor = [System.Drawing.Color]::FromArgb(45, 45, 68)

    for ($row = 0; $row -lt $rows; $row++) {
        for ($col = 0; $col -lt $cols; $col++) {
            $kx = $keyRect.X + 4 + ($col * ($keySize + $keyGap * 0.5))
            $ky = $keyRect.Y + 4 + ($row * ($keySize * 0.85))
            $keyBmpBrush = New-Object System.Drawing.SolidBrush($keyColor)
            $g.FillRectangle($keyBmpBrush, $kx, $ky, $keySize * 0.7, $keySize * 0.55)
            $keyBmpBrush.Dispose()
        }
    }

    # Space bar
    $spaceBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(61, 61, 85))
    $g.FillRectangle($spaceBrush,
        $keyRect.X + $keyW * 0.2,
        $keyRect.Y + $keyH - $keySize * 0.55 - 3,
        $keyW * 0.6,
        $keySize * 0.35)
    $spaceBrush.Dispose()

    # Globe icon above keyboard
    $globeCenterX = $size / 2
    $globeCenterY = $size * 0.2
    $globeRadius = $size * 0.12

    # Globe circle
    $globePen = New-Object System.Drawing.Pen($cyanLight, 2.5)
    $g.DrawEllipse($globePen,
        $globeCenterX - $globeRadius,
        $globeCenterY - $globeRadius,
        $globeRadius * 2,
        $globeRadius * 2)

    # Globe inner ellipse (latitude)
    $g.DrawEllipse($globePen,
        $globeCenterX - $globeRadius * 0.55,
        $globeCenterY - $globeRadius,
        $globeRadius * 1.1,
        $globeRadius * 2)

    # Latitude lines
    $latPen = New-Object System.Drawing.Pen($cyanLight, 1.2)
    for ($lat = -1; $lat -le 1; $lat += 1) {
        if ($lat -eq 0) { continue }
        $yOffset = $lat * $globeRadius * 0.5
        $xHalf = [Math]::Sqrt([Math]::Max(0, $globeRadius * $globeRadius - $yOffset * $yOffset))
        $g.DrawLine($latPen,
            $globeCenterX - $xHalf,
            $globeCenterY + $yOffset,
            $globeCenterX + $xHalf,
            $globeCenterY + $yOffset)
    }
    $latPen.Dispose()
    $globePen.Dispose()

    # Arrow pointing down from globe to keyboard
    $arrowPen = New-Object System.Drawing.Pen($cyanLight, 1.8)
    $arrowX = $globeCenterX
    $arrowY1 = $globeCenterY + $globeRadius + 3
    $arrowY2 = $keyRect.Y - 3
    $g.DrawLine($arrowPen, $arrowX, $arrowY1, $arrowX, $arrowY2)
    $g.DrawLine($arrowPen, $arrowX - 3, $arrowY2 - 4, $arrowX, $arrowY2)
    $g.DrawLine($arrowPen, $arrowX + 3, $arrowY2 - 4, $arrowX, $arrowY2)
    $arrowPen.Dispose()

    # "LK" letters at bottom
    $letterY = $size * 0.82
    $letterSize = $size * 0.1
    $letterGap = $size * 0.06

    $lX = $size / 2 - $letterGap / 2 - $letterSize * 1.1
    $kX = $size / 2 + $letterGap / 2

    # L key
    $lRect = New-Object System.Drawing.Drawing2D.GraphicsPath
    Add-RoundedRectPath -path $lRect -x $lX -y $letterY -w $letterSize -h $letterSize -r 3
    $lBrush = New-Object System.Drawing.SolidBrush($cyanLight)
    $g.FillPath($lBrush, $lRect)
    $lBrush.Dispose()

    # K key
    $kRect = New-Object System.Drawing.Drawing2D.GraphicsPath
    Add-RoundedRectPath -path $kRect -x $kX -y $letterY -w $letterSize -h $letterSize -r 3
    $kBrush = New-Object System.Drawing.SolidBrush($cyanLight)
    $g.FillPath($kBrush, $kRect)
    $kBrush.Dispose()

    # Draw "L" and "K" text
    $fontSize = $letterSize * 0.6
    $font = New-Object System.Drawing.Font('Segoe UI', $fontSize, [System.Drawing.FontStyle]::Bold)
    $textBrush = New-Object System.Drawing.SolidBrush($darkBg)
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = 'Center'
    $fmt.LineAlignment = 'Center'

    $g.DrawString('L', $font, $textBrush,
        (New-Object System.Drawing.RectangleF($lX, $letterY, $letterSize, $letterSize)), $fmt)
    $g.DrawString('K', $font, $textBrush,
        (New-Object System.Drawing.RectangleF($kX, $letterY, $letterSize, $letterSize)), $fmt)

    $font.Dispose()
    $textBrush.Dispose()
    $fmt.Dispose()
    $g.Dispose()

    return $bmp
}

# ============================================================
# Generate all required icons
# ============================================================

# 1. Square150x150Logo.png (150x150)
Write-Host "Generating Square150x150Logo.png..."
$img150 = New-LangKeepIcon -size 150
$img150.Save("$outputDir\Square150x150Logo.png", [System.Drawing.Imaging.ImageFormat]::Png)
$img150.Dispose()

# 2. Square44x44Logo.png (44x44)
Write-Host "Generating Square44x44Logo.png..."
$img44 = New-LangKeepIcon -size 44
$img44.Save("$outputDir\Square44x44Logo.png", [System.Drawing.Imaging.ImageFormat]::Png)
$img44.Dispose()

# 3. Square71x71Logo.png (71x71)
Write-Host "Generating Square71x71Logo.png..."
$img71 = New-LangKeepIcon -size 71
$img71.Save("$outputDir\Square71x71Logo.png", [System.Drawing.Imaging.ImageFormat]::Png)
$img71.Dispose()

# 4. Wide310x150Logo.png (310x150)
Write-Host "Generating Wide310x150Logo.png..."
$img310 = New-Object System.Drawing.Bitmap(310, 150)
$g310 = [System.Drawing.Graphics]::FromImage($img310)
$g310.SmoothingMode = 'HighQuality'
$g310.InterpolationMode = 'HighQualityBicubic'

# Wide tile background - rounded rect
$widePath = New-Object System.Drawing.Drawing2D.GraphicsPath
Add-RoundedRectPath -path $widePath -x 0 -y 0 -w 310 -h 150 -r 25
$wideBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point(0, 0)),
    (New-Object System.Drawing.Point(310, 150)),
    $blue1, $blue2)
$g310.FillPath($wideBrush, $widePath)
$wideBrush.Dispose()

# Draw "LangKeep" text
$wf = New-Object System.Drawing.Font('Segoe UI', 20, [System.Drawing.FontStyle]::Bold)
$wtBrush = New-Object System.Drawing.SolidBrush($white)
$wsf = New-Object System.Drawing.StringFormat
$wsf.Alignment = 'Near'
$wsf.LineAlignment = 'Center'
$g310.DrawString('LangKeep', $wf, $wtBrush, 80, 55)
$wf.Dispose()
$wtBrush.Dispose()

# Small keyboard icon on the left
$ikBrush = New-Object System.Drawing.SolidBrush($darkBg)
$g310.FillRectangle($ikBrush, 25, 45, 40, 60)
$ikBrush.Dispose()

# Small "LK" in the icon area
$lkFont = New-Object System.Drawing.Font('Segoe UI', 16, [System.Drawing.FontStyle]::Bold)
$lkBrush = New-Object System.Drawing.SolidBrush($cyanLight)
$lkFmt = New-Object System.Drawing.StringFormat
$lkFmt.Alignment = 'Center'
$lkFmt.LineAlignment = 'Center'
$g310.DrawString('LK', $lkFont, $lkBrush, 45, 75, $lkFmt)
$lkFont.Dispose()
$lkBrush.Dispose()
$lkFmt.Dispose()

# Tagline
$tagFont = New-Object System.Drawing.Font('Segoe UI', 9)
$tagBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(200, 255, 255, 255))
$g310.DrawString('Auto Language Switcher', $tagFont, $tagBrush, 82, 86)
$tagFont.Dispose()
$tagBrush.Dispose()

$g310.Dispose()
$img310.Save("$outputDir\Wide310x150Logo.png", [System.Drawing.Imaging.ImageFormat]::Png)
$img310.Dispose()

# 5. BadgeLogo.png (24x24 with WHITE pixel at 0,0)
Write-Host "Generating BadgeLogo.png..."
$bmpBadge = New-Object System.Drawing.Bitmap(24, 24)
$gBadge = [System.Drawing.Graphics]::FromImage($bmpBadge)
$gBadge.SmoothingMode = 'HighQuality'

# Fill with transparent
$gBadge.Clear($transparent)

# Set pixel (0,0) to white (required by WACK for badge logos)
$bmpBadge.SetPixel(0, 0, [System.Drawing.Color]::White)

# Draw a small keyboard icon
$badgeBrush = New-Object System.Drawing.SolidBrush($blue1)
$gBadge.FillRectangle($badgeBrush, 4, 7, 16, 10)
$badgeBrush.Dispose()

# Key rows
$keyCol = [System.Drawing.Color]::FromArgb(200, 255, 255, 255)
for ($row = 0; $row -lt 3; $row++) {
    for ($col = 0; $col -lt 5; $col++) {
        if ($row -eq 2 -and $col -gt 1) { break }
        $bmpBadge.SetPixel(6 + $col * 3, 9 + $row * 3, $keyCol)
    }
}
$gBadge.Dispose()
$bmpBadge.Save("$outputDir\BadgeLogo.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmpBadge.Dispose()

Write-Host ""
Write-Host "All icons generated successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "New files:" -ForegroundColor Cyan
Get-ChildItem $outputDir | Select-Object Name, Length | Format-Table -AutoSize
