# LangKeep MSIX Build Script
# Creates a local MSIX package for Store submission or manual installation.
# Usage: .\build-msix.ps1
# Prerequisites: Windows SDK (for MakeAppx.exe), .NET 9 SDK

param(
    [string]$Version = "0.2.2.0",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipSign
)

$ErrorActionPreference = "Stop"
$RepoRoot = $PSScriptRoot
$ArtifactsDir = Join-Path $RepoRoot "artifacts"
$PublishDir = Join-Path $ArtifactsDir "publish"
$StagingDir = Join-Path $ArtifactsDir "msix-staging"
$MsixPath = Join-Path $ArtifactsDir "LangKeep-$Version-x64.msix"
$ManifestSrc = Join-Path $RepoRoot "src\LangKeep.Packaging\Package.appxmanifest"
$ResourcesDir = Join-Path $RepoRoot "src\LangKeep.Packaging\Resources"
$ProjectWpf = Join-Path $RepoRoot "src\LangKeep.UI.Wpf\LangKeep.UI.Wpf.csproj"

# ───────────────────── Step 1: Publish ─────────────────────

Write-Host "=== Step 1: Publishing WPF app ($Configuration, $Runtime) ===" -ForegroundColor Cyan

& dotnet publish $ProjectWpf `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $PublishDir `
    -p:DebugType=none `
    -p:Version=$Version

if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
Write-Host "Publish succeeded." -ForegroundColor Green

# ───────────────────── Step 2: Stage files ─────────────────────

Write-Host "`n=== Step 2: Staging files for MSIX ===" -ForegroundColor Cyan

New-Item -ItemType Directory -Force -Path "$StagingDir\Resources" | Out-Null
Copy-Item -Path "$PublishDir\*" -Destination $StagingDir -Recurse -Force
Copy-Item -Path "$ResourcesDir\*" -Destination "$StagingDir\Resources\" -Recurse -Force

# Copy license
$LicenseSrc = Join-Path $RepoRoot "LICENSE"
if (Test-Path $LicenseSrc) {
    Copy-Item $LicenseSrc -Destination "$StagingDir\LICENSE.txt" -Force
}

Write-Host "Files staged at: $StagingDir" -ForegroundColor Green

# ───────────────────── Step 3: Resolve manifest ─────────────────────

Write-Host "`n=== Step 3: Resolving manifest placeholders ===" -ForegroundColor Cyan

$parts = $Version.Split('.')
while ($parts.Count -lt 4) { $parts += '0' }
$msixVersion = $parts[0..3] -join '.'

$content = [System.IO.File]::ReadAllText($ManifestSrc)
$content = $content.Replace('$targetnametoken$', 'LangKeep')
$content = $content.Replace('$targetentrypoint$', 'Windows.FullTrustApplication')
$content = $content -replace 'Version="\d+\.\d+\.\d+\.\d+"', "Version=`"$msixVersion`""
[System.IO.File]::WriteAllText("$StagingDir\AppxManifest.xml", $content)

Write-Host "Manifest version set to: $msixVersion" -ForegroundColor Green

# ───────────────────── Step 4: Create MSIX ─────────────────────

Write-Host "`n=== Step 4: Creating MSIX package ===" -ForegroundColor Cyan

$makeAppx = Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin" `
    -Recurse -Filter "MakeAppx.exe" `
    | Sort-Object FullName -Descending `
    | Select-Object -First 1 -ExpandProperty FullName

if (-not $makeAppx) {
    throw "MakeAppx.exe not found. Install Windows SDK."
}

Write-Host "Using MakeAppx: $makeAppx"
& $makeAppx pack /p $MsixPath /d $StagingDir /l 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "MSIX created: $MsixPath" -ForegroundColor Green
} else {
    throw "MakeAppx failed with exit code: $LASTEXITCODE"
}

# ───────────────────── Step 5: Sign (self-signed) ─────────────────────

if ($SkipSign) {
    Write-Host "`n=== Step 5: Skipped (SkipSign flag set) ===" -ForegroundColor Yellow
    Write-Host "MSIX is unsigned. Ready for Store submission (Store will re-sign)." -ForegroundColor Yellow
} else {
    Write-Host "`n=== Step 5: Signing MSIX with self-signed certificate ===" -ForegroundColor Cyan

$signtool = Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin" `
    -Filter "signtool.exe" -Recurse -Force `
    | Where-Object FullName -like '*\x64\signtool.exe' `
    | Sort-Object FullName -Descending `
    | Select-Object -First 1 -ExpandProperty FullName

if (-not $signtool) {
    Write-Host "WARNING: signtool.exe not found. MSIX will remain unsigned." -ForegroundColor Yellow
} else {
    Write-Host "Using signtool: $signtool"

    $pfxPath = "$env:TEMP\langkeep-cert.pfx"
    $pfxPassword = "LangKeepBuildCert2024"

    # Generate self-signed cert
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject "CN=Borislav Enchev, O=LangKeep" `
        -FriendlyName "LangKeep Build Certificate" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyExportPolicy Exportable `
        -KeyLength 2048 `
        -NotAfter (Get-Date).AddYears(3)

    $securePassword = ConvertTo-SecureString -String $pfxPassword -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $securePassword | Out-Null

    & $signtool sign /fd SHA256 /f $pfxPath /p $pfxPassword /v $MsixPath 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "MSIX signed successfully!" -ForegroundColor Green
    } else {
        Write-Host "WARNING: Signing failed with exit code: $LASTEXITCODE" -ForegroundColor Yellow
    }

    # Cleanup cert from store and temp
    Remove-Item "Cert:\CurrentUser\My\$($cert.Thumbprint)" -DeleteKey -Force -ErrorAction SilentlyContinue
    Remove-Item $pfxPath -Force -ErrorAction SilentlyContinue

    Write-Host "Certificate cleaned up." -ForegroundColor Gray
}
}

# ───────────────────── Done ─────────────────────

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "MSIX Package Created!" -ForegroundColor Green
Write-Host "  Path: $MsixPath" -ForegroundColor White
if (Test-Path $MsixPath) {
    $size = (Get-Item $MsixPath).Length
    Write-Host "  Size: $([math]::Round($size / 1MB, 2)) MB" -ForegroundColor White
}
Write-Host "`nFor Microsoft Store submission:" -ForegroundColor Yellow
Write-Host "  1. The Store will re-sign your package (no need to sign yourself)" -ForegroundColor Yellow
Write-Host "  2. You can upload the unsigned MSIX directly to Partner Center" -ForegroundColor Yellow
Write-Host "  3. Run WACK tests first: https://learn.microsoft.com/en-us/windows/apps/publish/store/windows-app-certification-kit" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan
