$ErrorActionPreference = "Stop"

Write-Host "Building Solution..." -ForegroundColor Cyan
dotnet build --nologo --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`nRunning Tests with Code Coverage..." -ForegroundColor Cyan

# Remove old results
if (Test-Path "TestResults") {
    Remove-Item "TestResults" -Recurse -Force
    Write-Host "Cleaned old test results" -ForegroundColor Yellow
}

# Run tests with coverage
Write-Host "`nExecuting tests..." -ForegroundColor Green
dotnet test SmartCampus.Tests/SmartCampus.Tests.csproj `
    --configuration Debug `
    --collect:"XPlat Code Coverage" `
    --results-directory ./TestResults `
    --verbosity normal `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile="**/Migrations/**/*.cs,**/CampusDbContext.cs,**/Program.cs"

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nTests failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`nGenerating Coverage Report..." -ForegroundColor Cyan

# Find the latest coverage file
$coverageFile = Get-ChildItem -Path "TestResults" -Recurse -Filter "coverage.cobertura.xml" | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -First 1

if ($null -eq $coverageFile) {
    Write-Host "Coverage file not found!" -ForegroundColor Red
    exit 1
}

Write-Host "Found coverage file: $($coverageFile.FullName)" -ForegroundColor Green

# Install reportgenerator if not exists
$reportGenPath = "reportgenerator"
if (-not (Get-Command $reportGenPath -ErrorAction SilentlyContinue)) {
    Write-Host "`nInstalling ReportGenerator tool..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-reportgenerator-globaltool
}

# Generate HTML report
Write-Host "`nGenerating HTML report..." -ForegroundColor Green
reportgenerator `
    -reports:"$($coverageFile.FullName)" `
    -targetdir:"TestResults/CoverageReport" `
    -reporttypes:"Html" `
    -classfilters:"-*Migrations*;-*DbContext*;-*Program*"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nCoverage report generated successfully!" -ForegroundColor Green
    Write-Host "Open TestResults/CoverageReport/index.html in your browser to view the report" -ForegroundColor Cyan
} else {
    Write-Host "`nReport generation had issues, but coverage data is available" -ForegroundColor Yellow
}

Write-Host "`nTest execution completed!" -ForegroundColor Green

