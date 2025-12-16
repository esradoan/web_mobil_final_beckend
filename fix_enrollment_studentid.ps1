# PowerShell script to fix Enrollment.StudentId
# This script uses MySQL .NET Connector to execute SQL

$connectionString = "Server=localhost;Database=smart_campus_db;User=root;Password=1234;Port=3306;"

# Try to load MySQL.Data.dll (if available)
$mysqlDllPath = "C:\Program Files (x86)\MySQL\MySQL Connector Net 8.0.36\Assemblies\v4.5.2\MySql.Data.dll"
if (Test-Path $mysqlDllPath) {
    Add-Type -Path $mysqlDllPath
} else {
    Write-Host "❌ MySQL.Data.dll not found. Please install MySQL Connector/NET or use MySQL Workbench/phpMyAdmin instead." -ForegroundColor Red
    Write-Host ""
    Write-Host "Alternative: Use MySQL Workbench or phpMyAdmin to run the SQL script manually." -ForegroundColor Yellow
    Write-Host "SQL file location: $PSScriptRoot\fix_enrollment_studentid.sql" -ForegroundColor Yellow
    exit 1
}

try {
    Write-Host "🔌 Connecting to database..." -ForegroundColor Cyan
    $connection = New-Object MySql.Data.MySqlClient.MySqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "✅ Connected successfully!" -ForegroundColor Green
    Write-Host ""
    
    # Read SQL script
    $sqlScript = Get-Content "$PSScriptRoot\fix_enrollment_studentid.sql" -Raw
    
    # Split by semicolon and execute each statement
    $statements = $sqlScript -split ';' | Where-Object { $_.Trim() -ne '' -and $_ -notmatch '^--' }
    
    foreach ($statement in $statements) {
        $statement = $statement.Trim()
        if ($statement -eq '') { continue }
        
        Write-Host "📝 Executing: $($statement.Substring(0, [Math]::Min(50, $statement.Length)))..." -ForegroundColor Cyan
        
        $command = $connection.CreateCommand()
        $command.CommandText = $statement
        
        if ($statement -match '^SELECT') {
            # For SELECT statements, display results
            $reader = $command.ExecuteReader()
            $results = @()
            while ($reader.Read()) {
                $row = @{}
                for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                    $row[$reader.GetName($i)] = $reader.GetValue($i)
                }
                $results += $row
            }
            $reader.Close()
            
            if ($results.Count -gt 0) {
                Write-Host "📊 Results:" -ForegroundColor Green
                $results | Format-Table -AutoSize
            }
        } else {
            # For UPDATE/INSERT/DELETE statements
            $rowsAffected = $command.ExecuteNonQuery()
            Write-Host "✅ Rows affected: $rowsAffected" -ForegroundColor Green
        }
        
        Write-Host ""
    }
    
    $connection.Close()
    Write-Host "✅ Script executed successfully!" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Alternative: Use MySQL Workbench or phpMyAdmin to run the SQL script manually." -ForegroundColor Yellow
    Write-Host "SQL file location: $PSScriptRoot\fix_enrollment_studentid.sql" -ForegroundColor Yellow
    exit 1
}

