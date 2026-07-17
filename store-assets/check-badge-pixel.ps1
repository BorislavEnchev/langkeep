Add-Type -AssemblyName System.Drawing
$path = "C:\Users\kiborko\desktop\github\langkeep\src\LangKeep.Packaging\Resources\BadgeLogo.png"
$img = [System.Drawing.Image]::FromFile($path)
$bmp = New-Object System.Drawing.Bitmap($img)
$c = $bmp.GetPixel(0, 0)
Write-Host ("Pixel(0,0): A=" + $c.A + " R=" + $c.R + " G=" + $c.G + " B=" + $c.B)
$bmp.Dispose()
$img.Dispose()
