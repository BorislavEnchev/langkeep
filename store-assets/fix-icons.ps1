# LangKeep — Generate WACK-compliant StoreLogo and BadgeLogo

Add-Type -AssemblyName 'System.Drawing'

$outputDir = "C:\Users\kiborko\desktop\github\langkeep\src\LangKeep.Packaging\Resources"

$blue1 = [System.Drawing.Color]::FromArgb(0, 120, 212)
$blue2 = [System.Drawing.Color]::FromArgb(16, 110, 190)
$white = [System.Drawing.Color]::White
$transparent = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)

# ============================================================
# 1. StoreLogo.png — 50x50
# WACK requires the logo referenced by <Logo> to pass size checks.
# Creating it at exactly 50x50 with the LangKeep brand.
# ============================================================
Write-Host "Generating StoreLogo.png (50x50)..."
$bmp = New-Object System.Drawing.Bitmap(50, 50)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'HighQuality'
$g.InterpolationMode = 'HighQualityBicubic'

# Blue rounded square background
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddArc(0, 0, 10, 10, 180, 90)
$path.AddArc(40, 0, 10, 10, 270, 90)
$path.AddArc(40, 40, 10, 10, 0, 90)
$path.AddArc(0, 40, 10, 10, 90, 90)
$path.CloseFigure()

$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point(0, 0)),
    (New-Object System.Drawing.Point(50, 50)),
    $blue1, $blue2)
$g.FillPath($brush, $path)
$brush.Dispose()

# "LK" text in center
$font = New-Object System.Drawing.Font('Segoe UI', 16, [System.Drawing.FontStyle]::Bold)
$textBrush = New-Object System.Drawing.SolidBrush($white)
$fmt = New-Object System.Drawing.StringFormat
$fmt.Alignment = 'Center'
$fmt.LineAlignment = 'Center'
$g.DrawString('LK', $font, $textBrush, 25, 25, $fmt)
$font.Dispose()
$textBrush.Dispose()
$fmt.Dispose()
$g.Dispose()
$bmp.Save("$outputDir\StoreLogo.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# ============================================================
# 2. BadgeLogo.png — 24x24, ALL pixels must be white or transparent
# WACK requires every pixel to be #FFFFFF or transparent (00######)
# ============================================================
Write-Host "Generating BadgeLogo.png (24x24, all-white/transparent)..."
$bmpBadge = New-Object System.Drawing.Bitmap(24, 24)
$gBadge = [System.Drawing.Graphics]::FromImage($bmpBadge)
$gBadge.Clear($transparent)

# Draw a simple monochrome keyboard shape using SetPixel (white only)
# Row 1: 5 small dots
for ($col = 0; $col -lt 5; $col++) {
    $bmpBadge.SetPixel(5 + $col * 3, 8, $white)
}
# Row 2: 5 small dots
for ($col = 0; $col -lt 5; $col++) {
    $bmpBadge.SetPixel(5 + $col * 3, 11, $white)
}
# Row 3: 5 small dots
for ($col = 0; $col -lt 5; $col++) {
    $bmpBadge.SetPixel(5 + $col * 3, 14, $white)
}
# Space bar (a wider line)
for ($col = -1; $col -le 1; $col++) {
    $bmpBadge.SetPixel(8 + $col * 3, 17, $white)
}

# Verify pixel (0,0) is transparent
$p0 = $bmpBadge.GetPixel(0, 0)
Write-Host "BadgeLogo pixel(0,0): A=$($p0.A) R=$($p0.R) G=$($p0.G) B=$($p0.B)"

$gBadge.Dispose()
$bmpBadge.Save("$outputDir\BadgeLogo.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmpBadge.Dispose()

Write-Host "Icons generated successfully!" -ForegroundColor Green

# Verify
$verifyFiles = @("StoreLogo.png", "BadgeLogo.png")
foreach ($name in $verifyFiles) {
    $vpath = "$outputDir\$name"
    if (Test-Path $vpath) {
        $vimg = [System.Drawing.Image]::FromFile($vpath)
        Write-Host "$name -> ${($vimg.Width)}x${($vimg.Height)}"
        $vimg.Dispose()
    }
}
