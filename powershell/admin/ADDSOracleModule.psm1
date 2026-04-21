# ADDS Oracle PowerShell Module
# Exported functions for ADDS administration

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Connect-ADDSOracle {
    param(
        [string]$Host = "ORACLE11G-PROD",
        [string]$Password = "adds_p@ss_2003!"
    )

    try {
        Add-Type -Path "C:\oracle\product\11.2.0\client_1\ODP.NET\bin\4\Oracle.DataAccess.dll" -ErrorAction Stop
        $connStr = "Data Source=$Host/$SID;User Id=$User;Password=$Password;"
        $conn = New-Object Oracle.DataAccess.Client.OracleConnection($connStr)
        $conn.Open()
        return $conn
    }
    catch {
        $err = $Error[0]
        throw "Connect-ADDSOracle failed: $($err.Exception.Message)"
    }
}

function Invoke-ADDSOracleQuery {
    $shouldClose = $false
    if ($null -eq $Connection) {
        $Connection = Connect-ADDSOracle
        $shouldClose = $true
    }

    try {
        $cmd = $Connection.CreateCommand()
        $cmd.CommandText = $Query
        $da = New-Object Oracle.DataAccess.Client.OracleDataAdapter($cmd)
        $dt = New-Object System.Data.DataTable
        $da.Fill($dt) | Out-Null
        return $dt
    }
    catch {
        $err = $Error[0]
        throw "Invoke-ADDSOracleQuery failed for query '$Query': $($err.Exception.Message)"
    }
    finally {
        if ($shouldClose) { $Connection.Close() }
    }
}

function Backup-ADDSDatabase {
    $date = Get-Date -Format "yyyyMMdd"
    $backupFile = "$BackupPath\adds_backup_$date.dmp"

    try {
        # Invoke-Expression with password in command line - security risk
        $expCmd = "exp $OracleUser/$OraclePass file=$backupFile log=$BackupPath\exp_$date.log"
        Invoke-Expression $expCmd -ErrorAction Stop
        Write-Host "Backup created: $backupFile"
        return $backupFile
    }
    catch {
        $err = $Error[0]
        throw "Backup-ADDSDatabase failed: $($err.Exception.Message)"
    }
}

function Restore-ADDSDatabase {
    )

    if (-not (Test-Path $DumpFile)) {
        throw "Dump file not found: $DumpFile"
    }

    try {
        $impCmd = "imp $OracleUser/$OraclePass file=$DumpFile fromuser=adds_user touser=adds_user"
        Invoke-Expression $impCmd -ErrorAction Stop
    }
    catch {
        $err = $Error[0]
        throw "Restore-ADDSDatabase failed: $($err.Exception.Message)"
    }
}

function Get-ADDSTableStats {

    $tables = @("EQUIPMENT", "INSTRUMENTS", "PIPE_ROUTES", "VESSELS", "HEAT_EXCHANGERS")
    $stats = @{}

    try {
        foreach ($table in $tables) {
            $dt = Invoke-ADDSOracleQuery -Query "SELECT COUNT(*) as CNT FROM $table" -Connection $Connection
            $stats[$table] = $dt.Rows[0]["CNT"]
        }
        return $stats
    }
    catch {
        $err = $Error[0]
        throw "Get-ADDSTableStats failed: $($err.Exception.Message)"
    }
}

function Reset-ADDSSequences {

    $sequences = @("SEQ_ROUTE", "SEQ_HX", "SEQ_INSTRUMENT")
    try {
        foreach ($seq in $sequences) {
            Invoke-ADDSOracleQuery -Query "ALTER SEQUENCE $seq RESTART START WITH 1" -Connection $Connection
        }
    }
    catch {
        $err = $Error[0]
        throw "Reset-ADDSSequences failed: $($err.Exception.Message)"
    }
}

