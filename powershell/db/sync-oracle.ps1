# ADDS Oracle Database Sync Script
# Runs nightly via Task Scheduler

param(
    [string]$OracleHost = "ORACLE11G-PROD",
    [string]$OracleSID = "ADDSDB",
    [string]$OracleUser = "adds_user",
    [string]$OraclePass = "adds_p@ss_2003!",
    [string]$LogPath = "C:\ADDS\Logs\sync.log"
)

Import-Module "C:\ADDS\Modules\ADDSOracleModule.psm1"
Import-Module "C:\oracle\product\11.2.0\client_1\ODP.NET\bin\4\Oracle.DataAccess.dll"

function Write-SyncLog {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "$timestamp [$Level] $Message" | Out-File $LogPath -Append
    Write-Host "$timestamp [$Level] $Message"
}

function Get-OracleConnection {
    $connStr = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=$OracleHost)(PORT=1521))" +
               "(CONNECT_DATA=(SID=$OracleSID)));User Id=$OracleUser;Password=$OraclePass;"
    $conn = New-Object Oracle.DataAccess.Client.OracleConnection($connStr)
    $conn.Open()
    return $conn
}

function Sync-EquipmentTable {
    param($Connection)

    Write-SyncLog "Syncing EQUIPMENT table..."

    $cmd = $Connection.CreateCommand()
    $cmd.CommandText = "SELECT TAG, TYPE, MODEL, MODIFIED FROM EQUIPMENT WHERE MODIFIED > SYSDATE - 1/24"
    $reader = $cmd.ExecuteReader()

    $count = 0
    while ($reader.Read()) {
        $tag = $reader["TAG"]
        $type = $reader["TYPE"]
        Update-LocalCache -Tag $tag -Type $type
        $count++
    }
    $reader.Close()
    Write-SyncLog "Synced $count equipment records"
}

function Update-LocalCache {
    param([string]$Tag, [string]$Type)

    $cachePath = "C:\ADDS\Cache\equipment_$Tag.xml"
    [xml]$doc = "<equipment><tag>$Tag</tag><type>$Type</type></equipment>"
    $doc.Save($cachePath)
}

function Sync-InstrumentReadings {
    param($Connection)

    Write-SyncLog "Syncing instrument readings..."

    $cmd = $Connection.CreateCommand()
    $cmd.CommandText = "SELECT TAG, LAST_VALUE, TIMESTAMP FROM INSTRUMENTS ORDER BY TIMESTAMP DESC"
    $da = New-Object Oracle.DataAccess.Client.OracleDataAdapter($cmd)
    $dt = New-Object System.Data.DataTable
    $da.Fill($dt) | Out-Null

    $dt | Export-Csv "C:\ADDS\Cache\instruments.csv" -NoTypeInformation
    Write-SyncLog "Exported $($dt.Rows.Count) instrument readings"
}

function Test-DatabaseHealth {
    param($Connection)

    $checks = @(
        "SELECT COUNT(*) FROM EQUIPMENT",
        "SELECT COUNT(*) FROM INSTRUMENTS",
        "SELECT COUNT(*) FROM PIPE_ROUTES"
    )

    foreach ($sql in $checks) {
        $cmd = $Connection.CreateCommand()
        $cmd.CommandText = $sql
        $result = $cmd.ExecuteScalar()
        Write-SyncLog "Health check [$sql]: $result rows"
    }
}

# Main sync
try {
    Write-SyncLog "Starting ADDS nightly sync"
    $conn = Get-OracleConnection
    Sync-EquipmentTable -Connection $conn
    Sync-InstrumentReadings -Connection $conn
    Test-DatabaseHealth -Connection $conn
    $conn.Close()
    Write-SyncLog "Sync completed successfully"
}
catch {
    Write-SyncLog "Sync failed: $_" -Level "ERROR"
    # Email alert using deprecated Send-MailMessage
    Send-MailMessage -To "admin@company.com" -From "adds@company.com" `
        -Subject "ADDS Sync Failed" -Body $_.ToString() `
        -SmtpServer "mail.company.com"
}
