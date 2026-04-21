# ADDS Oracle PowerShell Module
# Exported functions for ADDS administration

function Connect-ADDSOracle {
    param(
        [string]$Host = "ORACLE11G-PROD",
        [string]$SID = "ADDSDB",
        [string]$User = "adds_user",
        [string]$Password = "adds_p@ss_2003!"
    )

    Add-Type -Path "C:\oracle\product\11.2.0\client_1\ODP.NET\bin\4\Oracle.DataAccess.dll"
    $connStr = "Data Source=$Host/$SID;User Id=$User;Password=$Password;"
    $conn = New-Object Oracle.DataAccess.Client.OracleConnection($connStr)
    $conn.Open()
    return $conn
}

function Invoke-ADDSOracleQuery {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Query,
        $Connection
    )

    $shouldClose = $false
    if ($null -eq $Connection) {
        $Connection = Connect-ADDSOracle
        $shouldClose = $true
    }

    $cmd = $Connection.CreateCommand()
    $cmd.CommandText = $Query
    $da = New-Object Oracle.DataAccess.Client.OracleDataAdapter($cmd)
    $dt = New-Object System.Data.DataTable
    $da.Fill($dt) | Out-Null

    if ($shouldClose) { $Connection.Close() }
    return $dt
}

function Backup-ADDSDatabase {
    param(
        [string]$BackupPath = "C:\ADDS\Backups",
        [string]$OracleUser = "adds_user",
        [string]$OraclePass = "adds_p@ss_2003!"
    )

    $date = Get-Date -Format "yyyyMMdd"
    $backupFile = "$BackupPath\adds_backup_$date.dmp"

    # Invoke-Expression with password in command line - security risk
    $expCmd = "exp $OracleUser/$OraclePass file=$backupFile log=$BackupPath\exp_$date.log"
    Invoke-Expression $expCmd

    Write-Host "Backup created: $backupFile"
    return $backupFile
}

function Restore-ADDSDatabase {
    param(
        [string]$DumpFile,
        [string]$OracleUser = "adds_user",
        [string]$OraclePass = "adds_p@ss_2003!"
    )

    if (-not (Test-Path $DumpFile)) {
        throw "Dump file not found: $DumpFile"
    }

    $impCmd = "imp $OracleUser/$OraclePass file=$DumpFile fromuser=adds_user touser=adds_user"
    Invoke-Expression $impCmd
}

function Get-ADDSTableStats {
    param($Connection)

    $tables = @("EQUIPMENT", "INSTRUMENTS", "PIPE_ROUTES", "VESSELS", "HEAT_EXCHANGERS")
    $stats = @{}

    foreach ($table in $tables) {
        $dt = Invoke-ADDSOracleQuery -Query "SELECT COUNT(*) as CNT FROM $table" -Connection $Connection
        $stats[$table] = $dt.Rows[0]["CNT"]
    }

    return $stats
}

function Reset-ADDSSequences {
    param($Connection)

    $sequences = @("SEQ_ROUTE", "SEQ_HX", "SEQ_INSTRUMENT")
    foreach ($seq in $sequences) {
        Invoke-ADDSOracleQuery -Query "ALTER SEQUENCE $seq RESTART START WITH 1" -Connection $Connection
    }
}

Export-ModuleMember -Function Connect-ADDSOracle, Invoke-ADDSOracleQuery,
    Backup-ADDSDatabase, Restore-ADDSDatabase, Get-ADDSTableStats, Reset-ADDSSequences
