[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$Platform = "x64"
)

$projectPath = Join-Path $PSScriptRoot "..\src\Wallpaper233.App\Wallpaper233.App.csproj"

dotnet restore $projectPath -r $RuntimeIdentifier
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

dotnet publish $projectPath -c $Configuration -r $RuntimeIdentifier -p:Platform=$Platform -p:PublishAot=true -p:PublishTrimmed=true --no-restore
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

Write-Host "Experimental NativeAOT publish completed for $RuntimeIdentifier."
