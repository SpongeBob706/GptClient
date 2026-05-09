param(
    [Parameter(Mandatory=$true)]
    [string]$ApiKey,
    
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

# Очистка
Write-Host "Cleaning..." -ForegroundColor Yellow
dotnet clean --configuration Release

# Явная сборка
Write-Host "Building..." -ForegroundColor Yellow
dotnet build --configuration Release

# Упаковка (используем уже собранные DLL)
Write-Host "Packing..." -ForegroundColor Yellow
dotnet pack --configuration Release --no-build -o ./nupkgs /p:Version=$Version

# Проверяем, что пакет создался
if (-not (Test-Path ".\nupkgs")) {
    Write-Host "Output folder not found!" -ForegroundColor Red
    exit 1
}

$package = Get-ChildItem -Path ".\nupkgs" -Filter "*.nupkg" | Select-Object -First 1

if ($null -eq $package) {
    Write-Host "Package not found in .\nupkgs!" -ForegroundColor Red
    exit 1
}

Write-Host "Found package: $($package.Name)" -ForegroundColor Green

# Публикация
Write-Host "Publishing..." -ForegroundColor Yellow
dotnet nuget push $package.FullName --api-key $ApiKey --source https://api.nuget.org/v3/index.json

Write-Host "Package $Version published successfully!" -ForegroundColor Green