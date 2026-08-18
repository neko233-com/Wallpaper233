[CmdletBinding()]
param(
    [string]$PublishDirectory,
    [string]$InstallDirectory
)

if ([string]::IsNullOrWhiteSpace($PublishDirectory)) {
    $PublishDirectory = Join-Path $PSScriptRoot "..\src\Wallpaper233.App\bin\Release\jit-self-contained"
}

if ([string]::IsNullOrWhiteSpace($InstallDirectory)) {
    $InstallDirectory = Join-Path $env:LOCALAPPDATA "Wallpaper233\current"
}

$resolvedPublishDirectory = (Resolve-Path -LiteralPath $PublishDirectory -ErrorAction Stop).Path
$executablePath = Join-Path $resolvedPublishDirectory "Wallpaper233.App.exe"
if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "Wallpaper233 executable not found: $executablePath"
}

New-Item -ItemType Directory -Force -Path $InstallDirectory | Out-Null
Copy-Item -Path (Join-Path $resolvedPublishDirectory "*") -Destination $InstallDirectory -Recurse -Force

Write-Host "Installed Wallpaper233 to $InstallDirectory"
Write-Host "Executable: $(Join-Path $InstallDirectory 'Wallpaper233.App.exe')"
