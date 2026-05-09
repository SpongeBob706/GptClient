param(
    [Parameter(Mandatory=$true)]
    [string]$ApiKey,

    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host "Cleaning..." -ForegroundColor Yellow
dotnet clean --configuration Release

Remove-Item ".\nupkgs\*" -Force -ErrorAction SilentlyContinue

Write-Host "Building..." -ForegroundColor Yellow
dotnet build --configuration Release

Write-Host "Packing..." -ForegroundColor Yellow
dotnet pack `
    --configuration Release `
    --no-build `
    -o ./nupkgs `
    /p:Version=$Version

if (-not (Test-Path ".\nupkgs")) {
    throw "Output folder not found"
}

$package = Get-ChildItem `
    -Path ".\nupkgs" `
    -Filter "*$Version*.nupkg" |
    Select-Object -First 1

if ($null -eq $package) {
    throw "Package $Version not found"
}

Write-Host "Found package: $($package.Name)" -ForegroundColor Green

Write-Host "Publishing..." -ForegroundColor Yellow

dotnet nuget push $package.FullName `
    --api-key $ApiKey `
    --source https://api.nuget.org/v3/index.json `
    --skip-duplicate

if ($LASTEXITCODE -ne 0) {
    throw "NuGet publish failed"
}

Write-Host "Package $Version published successfully!" -ForegroundColor Green