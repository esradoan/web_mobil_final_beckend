# Backend Test Coverage Script
# Usage: .\run-tests-backend.ps1

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Backend Test Coverage Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build Solution
Write-Host "[1/4] Building Solution..." -ForegroundColor Yellow
dotnet build --nologo --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build successful" -ForegroundColor Green
Write-Host ""

# Step 2: Clean old results
Write-Host "[2/4] Cleaning old test results..." -ForegroundColor Yellow
if (Test-Path "TestResults") {
    Remove-Item "TestResults" -Recurse -Force
    Write-Host "✅ Old results cleaned" -ForegroundColor Green
} else {
    Write-Host "ℹ️  No old results to clean" -ForegroundColor Gray
}
Write-Host ""

# Step 3: Run tests with coverage
Write-Host "[3/4] Running tests with code coverage..." -ForegroundColor Yellow
Write-Host ""

dotnet test SmartCampus.Tests/SmartCampus.Tests.csproj `
    --configuration Debug `
    --collect:"XPlat Code Coverage" `
    --results-directory ./TestResults `
    --verbosity minimal `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile="**/Migrations/**/*.cs,**/CampusDbContext.cs,**/Program.cs,**/DbMigrationHelper.cs"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ Tests failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Tests completed successfully" -ForegroundColor Green
Write-Host ""

# Step 4: Generate HTML coverage report
Write-Host "[4/4] Generating coverage report..." -ForegroundColor Yellow

# Find the latest coverage file
$coverageFile = Get-ChildItem -Path "TestResults" -Recurse -Filter "coverage.cobertura.xml" | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -First 1

if ($null -eq $coverageFile) {
    Write-Host "❌ Coverage file not found!" -ForegroundColor Red
    exit 1
}

Write-Host "📄 Found coverage file: $($coverageFile.Name)" -ForegroundColor Gray

# Check if reportgenerator is installed
$reportGenPath = "reportgenerator"
if (-not (Get-Command $reportGenPath -ErrorAction SilentlyContinue)) {
    Write-Host "📦 Installing ReportGenerator tool..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-reportgenerator-globaltool
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to install ReportGenerator!" -ForegroundColor Red
        Write-Host "💡 Try running manually: dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Yellow
        exit 1
    }
}

# Generate HTML report
Write-Host "📊 Generating HTML report..." -ForegroundColor Gray
reportgenerator `
    -reports:"$($coverageFile.FullName)" `
    -targetdir:"TestResults/CoverageReport" `
    -reporttypes:"Html" `
    -classfilters:"-*Migrations*;-*DbContext*;-*Program*;-*DbMigrationHelper*"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  ✅ Coverage Report Generated!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "📁 Report Location:" -ForegroundColor Cyan
    Write-Host "   TestResults/CoverageReport/index.html" -ForegroundColor White
    Write-Host ""
    Write-Host "💡 To open the report:" -ForegroundColor Yellow
    Write-Host "   Start-Process TestResults/CoverageReport/index.html" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "⚠️  Report generation had issues, but coverage data is available" -ForegroundColor Yellow
    Write-Host "   Coverage file: $($coverageFile.FullName)" -ForegroundColor Gray
}

Write-Host ""

