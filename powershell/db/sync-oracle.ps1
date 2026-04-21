#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

<#
.SYNOPSIS
    ADDS Oracle Database Sync — runs nightly via Task Scheduler.
.DESCRIPTION
    Modernized: ODP.NET managed 19c, credentials from environment, strict mode,
    Send-MailMessage replaced with Net.Mail.SmtpClient, no hardcoded passwords.
#>

param(
    [string]$LogPath = 'C:\ADDS\Logs\sync.log'
)

Import-Module 'C:\ADDS\Modules\ADDSOracleModule.psm1'
Add-Type -Path 'C:\oracle\product\19\client_1\odp.net\managed\common\Oracle.ManagedDataAccess.dll'

function Write-SyncLog {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $line = "$ts [$Level] $Message"
    $line | Out-File -FilePath $LogPath -Append -Encoding UTF8
    Write-Information $line -InformationAction Continue
}

function Connect-SyncOracle {
    # Credentials resolved in ADDSOracleModule from env vars — not passed here
    return Connect-ADDSOracle `
        -Host ($env:ADDS_ORACLE_HOST ?? 'ORACLE19C-PROD') `
        -SID  ($env:ADDS_ORACLE_SID  ?? 'ADDSDB')
}

function Sync-EquipmentTable {
    [CmdletBinding()]
    param([Oracle.ManagedDataAccess.Client.OracleConnection]$Connection)

    Write-SyncLog 'Syncing EQUIPMENT table...'

    $cmd = $Connection.CreateCommand()
    $cmd.CommandText = 'SELECT TAG, TYPE, MODEL, MODIFIED FROM EQUIPMENT WHERE MODIFIED > SYSDATE - 1/24'
    $reader = $cmd.ExecuteReader()

    $cacheDir = 'C:\ADDS\Cache'
    if (-not (Test-Path $cacheDir)) { New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null }

    $count = 0
    while ($reader.Read()) {
        $tag  = [string]$reader['TAG']
        $type = [string]$reader['TYPE']
        Update-LocalCache -Tag $tag -Type $type -CacheDir $cacheDir
        $count++
    }
    $reader.Close()
    Write-SyncLog "Synced $count equipment records"
}

function Update-LocalCache {
    param([string]$Tag, [string]$Type, [string]$CacheDir)

    # XmlDocument construction — no string interpolation into XML
    $doc  = New-Object System.Xml.XmlDocument
    $root = $doc.CreateElement('equipment')
    $tagEl  = $doc.CreateElement('tag');  $tagEl.InnerText  = $Tag;  $root.AppendChild($tagEl)  | Out-Null
    $typeEl = $doc.CreateElement('type'); $typeEl.InnerText = $Type; $root.AppendChild($typeEl) | Out-Null
    $doc.AppendChild($root) | Out-Null
    $doc.Save((Join-Path $CacheDir "equipment_$Tag.xml"))
}

function Sync-InstrumentReadings {
    [CmdletBinding()]
    param([Oracle.ManagedDataAccess.Client.OracleConnection]$Connection)

    Write-SyncLog 'Syncing instrument readings...'

    $dt = Invoke-ADDSOracleQuery `
        -Query 'SELECT TAG, LAST_VALUE, TIMESTAMP FROM INSTRUMENTS ORDER BY TIMESTAMP DESC' `
        -Connection $Connection

    $csvPath = 'C:\ADDS\Cache\instruments.csv'
    $dt | Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8
    Write-SyncLog "Exported $($dt.Rows.Count) instrument readings"
}

function Test-DatabaseHealth {
    [CmdletBinding()]
    param([Oracle.ManagedDataAccess.Client.OracleConnection]$Connection)

    $tables = @('EQUIPMENT','INSTRUMENTS','PIPE_ROUTES')
    foreach ($table in $tables) {
        $dt = Invoke-ADDSOracleQuery -Query "SELECT COUNT(*) AS CNT FROM $table" -Connection $Connection
        Write-SyncLog "Health [$table]: $([int]$dt.Rows[0]['CNT']) rows"
    }
}

function Send-SyncAlert {
    param([string]$ErrorMessage)

    $smtp   = $env:ADDS_SMTP_HOST   ?? 'mail.company.com'
    $from   = $env:ADDS_ALERT_FROM  ?? 'adds@company.com'
    $to     = $env:ADDS_ALERT_TO    ?? 'admin@company.com'

    try {
        $client  = New-Object System.Net.Mail.SmtpClient($smtp)
        $message = New-Object System.Net.Mail.MailMessage($from, $to,
            'ADDS Sync Failed', $ErrorMessage)
        $client.Send($message)
    }
    catch {
        Write-SyncLog "Failed to send alert email: $_" -Level 'WARN'
    }
}

# ── Main ─────────────────────────────────────────────────────────────────────

try {
    Write-SyncLog 'Starting ADDS nightly sync'
    $conn = Connect-SyncOracle
    Sync-EquipmentTable      -Connection $conn
    Sync-InstrumentReadings  -Connection $conn
    Test-DatabaseHealth      -Connection $conn
    $conn.Close()
    Write-SyncLog 'Sync completed successfully'
}
catch {
    Write-SyncLog "Sync failed: $_" -Level 'ERROR'
    Send-SyncAlert -ErrorMessage $_.ToString()
    exit 1
}
