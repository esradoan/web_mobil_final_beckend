$ErrorActionPreference = "Stop"

Write-Host "Building Solution..." -ForegroundColor Cyan
dotnet build --nologo --verbosity quiet

Write-Host "`nRunning Tests with Code Coverage..." -ForegroundColor Cyan

# Remove old results
if (Test-Path "TestResults") {
    Remove-Item "TestResults" -Recurse -Force
}

# Run tests
dotnet test SmartCampus.Tests/SmartCampus.Tests.csproj `
    --configuration Debug `
    --collect:"XPlat Code Coverage" `
    --results-directory ./TestResults `
    --verbosity normal `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile="**/Migrations/**/*.cs,**/CampusDbContext.cs"

Write-Host "`nTests Completed." -ForegroundColor Green

# Check for ReportGenerator
$reportGenerator = Get-Command reportgenerator -ErrorAction SilentlyContinue

if ($reportGenerator) {
    Write-Host "`nGenerating HTML Report..." -ForegroundColor Cyan
    reportgenerator `
        -reports:./TestResults/**/coverage.cobertura.xml `
        -targetdir:./TestResults/CoverageReport `
        -reporttypes:Html

    Write-Host "Report generated at: ./TestResults/CoverageReport/index.html" -ForegroundColor Green
}
else {
    Write-Host "`nReportGenerator tool not found. XML coverage file generated in ./TestResults." -ForegroundColor Yellow
    Write-Host "   To generate HTML reports, install the tool via:" -ForegroundColor Gray
    Write-Host "   dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Gray
}
