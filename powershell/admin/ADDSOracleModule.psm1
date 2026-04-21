#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

<#
.SYNOPSIS
    ADDS Oracle PowerShell Module — Oracle 19c managed driver, strict mode, approved verbs.
.DESCRIPTION
    Modernized: ODP.NET managed 19c, credentials from environment / SecureString,
    Invoke-Expression removed from backup/restore, strict mode enforced.
#>

function Connect-ADDSOracle {
    [CmdletBinding()]
    param(
        [string]$Host     = ($env:ADDS_ORACLE_HOST ?? 'ORACLE19C-PROD'),
        [string]$SID      = ($env:ADDS_ORACLE_SID  ?? 'ADDSDB'),
        [System.Management.Automation.PSCredential]$Credential
    )

    if ($null -eq $Credential) {
        $user = $env:ADDS_ORACLE_USER
        $pass = $env:ADDS_ORACLE_PASS
        if (-not $user -or -not $pass) {
            throw "Provide -Credential or set ADDS_ORACLE_USER / ADDS_ORACLE_PASS environment variables."
        }
        $Credential = New-Object System.Management.Automation.PSCredential(
            $user, (ConvertTo-SecureString $pass -AsPlainText -Force))
    }

    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Credential.Password)
    try {
        $plainPass = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
        $connStr = "Data Source=$Host/$SID;User Id=$($Credential.UserName);Password=$plainPass;"
        $conn = New-Object Oracle.ManagedDataAccess.Client.OracleConnection($connStr)
        $conn.Open()
        return $conn
    }
    finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Invoke-ADDSOracleQuery {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$Query,

        [Oracle.ManagedDataAccess.Client.OracleConnection]$Connection
    )

    $shouldClose = $null -eq $Connection
    if ($shouldClose) { $Connection = Connect-ADDSOracle }

    try {
        $cmd = $Connection.CreateCommand()
        $cmd.CommandText = $Query
        $da = New-Object Oracle.ManagedDataAccess.Client.OracleDataAdapter($cmd)
        $dt = New-Object System.Data.DataTable
        $da.Fill($dt) | Out-Null
        return $dt
    }
    finally {
        if ($shouldClose) { $Connection.Close() }
    }
}

function Backup-ADDSDatabase {
    [CmdletBinding()]
    param(
        [string]$BackupPath = 'C:\ADDS\Backups',
        [System.Management.Automation.PSCredential]$Credential
    )

    if ($null -eq $Credential) { $Credential = _Get-DefaultCredential }

    if (-not (Test-Path $BackupPath)) {
        New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null
    }

    $date       = Get-Date -Format 'yyyyMMdd'
    $backupFile = Join-Path $BackupPath "adds_backup_$date.dmp"
    $logFile    = Join-Path $BackupPath "exp_$date.log"

    # Use process object — no Invoke-Expression, no password in command line
    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Credential.Password)
    try {
        $plainPass = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
        $proc = Start-Process -FilePath 'expdp' `
            -ArgumentList "$($Credential.UserName)/$plainPass", `
                "dumpfile=$backupFile", "logfile=$logFile" `
            -Wait -PassThru -NoNewWindow
        if ($proc.ExitCode -ne 0) {
            throw "expdp exited with code $($proc.ExitCode). See $logFile."
        }
    }
    finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }

    Write-Verbose "Backup created: $backupFile"
    return $backupFile
}

function Restore-ADDSDatabase {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$DumpFile,

        [System.Management.Automation.PSCredential]$Credential
    )

    if (-not (Test-Path $DumpFile)) { throw "Dump file not found: $DumpFile" }
    if ($null -eq $Credential) { $Credential = _Get-DefaultCredential }

    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Credential.Password)
    try {
        $plainPass = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
        $proc = Start-Process -FilePath 'impdp' `
            -ArgumentList "$($Credential.UserName)/$plainPass", `
                "dumpfile=$DumpFile", "remap_schema=adds_user:adds_user" `
            -Wait -PassThru -NoNewWindow
        if ($proc.ExitCode -ne 0) { throw "impdp exited with code $($proc.ExitCode)." }
    }
    finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Get-ADDSTableStats {
    [CmdletBinding()]
    param(
        [Oracle.ManagedDataAccess.Client.OracleConnection]$Connection
    )

    $tables = @('EQUIPMENT','INSTRUMENTS','PIPE_ROUTES','VESSELS','HEAT_EXCHANGERS')
    $stats  = [ordered]@{}
    foreach ($table in $tables) {
        $dt = Invoke-ADDSOracleQuery -Query "SELECT COUNT(*) AS CNT FROM $table" -Connection $Connection
        $stats[$table] = [int]$dt.Rows[0]['CNT']
    }
    return $stats
}

function Reset-ADDSSequences {
    [CmdletBinding()]
    param(
        [Oracle.ManagedDataAccess.Client.OracleConnection]$Connection
    )

    $sequences = @('SEQ_ROUTE','SEQ_HX','SEQ_INSTRUMENT')
    foreach ($seq in $sequences) {
        Invoke-ADDSOracleQuery -Query "ALTER SEQUENCE $seq RESTART START WITH 1" -Connection $Connection | Out-Null
    }
}

# Internal helper — not exported
function _Get-DefaultCredential {
    $user = $env:ADDS_ORACLE_USER
    $pass = $env:ADDS_ORACLE_PASS
    if (-not $user -or -not $pass) {
        throw "Provide -Credential or set ADDS_ORACLE_USER / ADDS_ORACLE_PASS."
    }
    return New-Object System.Management.Automation.PSCredential(
        $user, (ConvertTo-SecureString $pass -AsPlainText -Force))
}

Export-ModuleMember -Function Connect-ADDSOracle, Invoke-ADDSOracleQuery,
    Backup-ADDSDatabase, Restore-ADDSDatabase, Get-ADDSTableStats, Reset-ADDSSequences
