# LangKeep — Generate Application .ico File

Add-Type -AssemblyName 'System.Drawing'

$outputPath = "C:\Users\kiborko\Desktop\GitHub\langkeep\src\LangKeep.UI.Wpf\Resources\LangKeep.ico"

function Add-RoundedRectPath {
    param([System.Drawing.Drawing2D.GraphicsPath]$path, [int]$x, [int]$y, [int]$w, [int]$h, [int]$r)
    $path.AddArc($x, $y, $r * 2, $r * 2, 180, 90)
    $path.AddArc($x + $w - $r * 2, $y, $r * 2, $r * 2, 270, 90)
    $path.AddArc($x + $w - $r * 2, $y + $h - $r * 2, $r * 2, $r * 2, 0, 90)
    $path.AddArc($x, $y + $h - $r * 2, $r * 2, $r * 2, 90, 90)
    $path.CloseFigure()
}

$blue1 = [System.Drawing.Color]::FromArgb(0, 120, 212)
$blue2 = [System.Drawing.Color]::FromArgb(16, 110, 190)
$cyanLight = [System.Drawing.Color]::FromArgb(80, 230, 255)
$darkBg = [System.Drawing.Color]::FromArgb(26, 26, 46)

function New-LangKeepIcon {
    param([int]$iconSize)

    $bmp = New-Object System.Drawing.Bitmap($iconSize, $iconSize)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'HighQuality'
    $g.InterpolationMode = 'HighQualityBicubic'

    # Background rounded square
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $corner = [Math]::Max(1, [int]($iconSize / 6))
    Add-RoundedRectPath -path $path -x 0 -y 0 -w $iconSize -h $iconSize -r $corner

    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point(0, 0)),
        (New-Object System.Drawing.Point($iconSize, $iconSize)),
        $blue1, $blue2)
    $g.FillPath($brush, $path)
    $brush.Dispose()

    # Keyboard body - big, may clip at edges
    $kw = [int]($iconSize * 1.1)
    $kh = [int]($kw * 0.5)
    $kx = [int](($iconSize - $kw) / 2)
    $ky = [int](($iconSize - $kh) / 2)

    $kPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    Add-RoundedRectPath -path $kPath -x $kx -y $ky -w $kw -h $kh -r 5
    $kBrush = New-Object System.Drawing.SolidBrush($darkBg)
    $g.FillPath($kBrush, $kPath)
    $kBrush.Dispose()

    # Key rows
    $rows = 4
    $cols = 6
    $keyGapW = [int]($kw * 0.03)
    $keyGapH = [int]($kh * 0.04)
    $keyW = [int](($kw - ($cols + 1) * $keyGapW) / $cols)
    $keyH = [int](($kh - ($rows + 1) * $keyGapH) / $rows)
    $keyColor = [System.Drawing.Color]::FromArgb(220, 230, 240)

    for ($row = 0; $row -lt $rows; $row++) {
        $colCount = if ($row -eq 3) { 3 } else { $cols }
        $colOffset = if ($row -eq 3) { [int](($cols - 3) * ($keyW + $keyGapW) / 2) } else { 0 }
        for ($col = 0; $col -lt $colCount; $col++) {
            $rx = $kx + $keyGapW + $col * ($keyW + $keyGapW) + $colOffset
            $ry = $ky + $keyGapH + $row * ($keyH + $keyGapH)
            $keyBmpBrush = New-Object System.Drawing.SolidBrush($keyColor)
            $g.FillRectangle($keyBmpBrush, $rx, $ry, $keyW, $keyH)
            $keyBmpBrush.Dispose()
        }
    }

    $g.Dispose()
    return $bmp
}

# Generate images at standard icon sizes
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = @{}

foreach ($size in $sizes) {
    Write-Host "Generating ${size}x${size}..."
    $images[$size] = New-LangKeepIcon -iconSize $size
}

# Build ICO file
$icoStream = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter($icoStream)

$writer.Write([byte]0)
$writer.Write([byte]0)
$writer.Write([System.Int16]1)
$writer.Write([System.Int16]$sizes.Count)

$imageData = @{}
$offsets = @{}
$currentOffset = 6 + $sizes.Count * 16

foreach ($size in $sizes) {
    $ms = New-Object System.IO.MemoryStream
    $images[$size].Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $imageData[$size] = $ms.ToArray()
    $ms.Dispose()
    $offsets[$size] = $currentOffset
    $currentOffset += $imageData[$size].Length
}

foreach ($size in $sizes) {
    if ($size -ge 256) { $w = 0 } else { $w = $size }
    if ($size -ge 256) { $h = 0 } else { $h = $size }
    $writer.Write([byte]$w)
    $writer.Write([byte]$h)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([System.Int16]1)
    $writer.Write([System.Int16]32)
    $writer.Write([int]$imageData[$size].Length)
    $writer.Write([int]$offsets[$size])
}

foreach ($size in $sizes) {
    $writer.Write($imageData[$size])
}

$writer.Flush()
[System.IO.File]::WriteAllBytes($outputPath, $icoStream.ToArray())
$writer.Close()
$icoStream.Dispose()

foreach ($size in $sizes) {
    $images[$size].Dispose()
}

Write-Host ""
Write-Host "Icon generated successfully: $outputPath" -ForegroundColor Green
