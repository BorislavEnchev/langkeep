Add-Type -AssemblyName 'System.Drawing'
$resources = @("Square150x150Logo","Square44x44Logo","Square71x71Logo","Wide310x150Logo","BadgeLogo")
foreach ($name in $resources) {
    $path = "C:\Users\kiborko\desktop\github\langkeep\src\LangKeep.Packaging\Resources\$name.png"
    if (Test-Path $path) {
        $img = [System.Drawing.Image]::FromFile($path)
        $w = $img.Width
        $h = $img.Height
        Write-Host "$name = ${w}x${h}"
        $img.Dispose()
    } else {
        Write-Host "$name = NOT FOUND"
    }
}
