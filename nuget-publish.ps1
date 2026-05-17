param()

$ErrorActionPreference = "Stop"

# Load .env
$envFile = Join-Path $PSScriptRoot ".env"
if (-not (Test-Path $envFile)) {
    throw ".env file not found at $envFile"
}

Get-Content $envFile | ForEach-Object {
    if ($_ -match '^\s*([^#][^=]*?)\s*=\s*(.*)\s*$') {
        [System.Environment]::SetEnvironmentVariable($Matches[1], $Matches[2])
    }
}

$ApiKey = $env:API_KEY
if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    throw "API_KEY not found in .env"
}

$Version = $env:NEXT_VERSION
if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "NEXT_VERSION not found in .env"
}

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

# Bump patch version and write back to .env
$parts = $Version -split '\.'
$parts[2] = [string]([int]$parts[2] + 1)
$newVersion = $parts -join '.'

$envContent = Get-Content $envFile
$envContent = $envContent -replace '^NEXT_VERSION=.*', "NEXT_VERSION=$newVersion"
Set-Content $envFile $envContent

Write-Host "Package $Version published successfully!" -ForegroundColor Green
Write-Host "Version bumped: $Version -> $newVersion" -ForegroundColor Cyan