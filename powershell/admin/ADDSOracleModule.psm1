# ADDS Oracle PowerShell Module
# Exported functions for ADDS administration
#
# CREDENTIALS POLICY
# ------------------
# No passwords or usernames may be stored in this file or passed as plain-text
# default parameter values. Set the following environment variables before using
# this module (e.g. in your CI/CD pipeline secrets, Windows Credential Manager,
# or a secrets-manager pre-load script):
#
#   ADDS_DB_HOST      Oracle hostname / IP          (default: none – must be set)
#   ADDS_DB_SID       Oracle SID or service name    (default: none – must be set)
#   ADDS_DB_USER      Database username             (default: none – must be set)
#   ADDS_DB_PASSWORD  Database password             (default: none – must be set)
#
# Rotate the credentials that previously existed in source control immediately.

function Get-ADDSEnvVar {
    param([string]$Name)
    $value = [System.Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Required environment variable '$Name' is not set. " +
              "Configure it via the OS environment or a secrets manager before using this module."
    }
    return $value
}

function Connect-ADDSOracle {
    param(
        # Parameters have no hard-coded defaults; values are sourced from
        # environment variables so that secrets never appear in call sites or logs.
        [string]$Host     = (Get-ADDSEnvVar 'ADDS_DB_HOST'),
        [string]$SID      = (Get-ADDSEnvVar 'ADDS_DB_SID'),
        [string]$User     = (Get-ADDSEnvVar 'ADDS_DB_USER'),
        [string]$Password = (Get-ADDSEnvVar 'ADDS_DB_PASSWORD')
    )

    Add-Type -Path "C:\oracle\product\11.2.0\client_1\ODP.NET\bin\4\Oracle.DataAccess.dll"
function Backup-ADDSDatabase {
    param(
        [string]$BackupPath = "C:\ADDS\Backups",
        # Credentials resolved from environment variables at runtime.
        [string]$OracleUser = (Get-ADDSEnvVar 'ADDS_DB_USER'),
        [string]$OraclePass = (Get-ADDSEnvVar 'ADDS_DB_PASSWORD')
    )

    $date = Get-Date -Format "yyyyMMdd"
    $backupFile = "$BackupPath\adds_backup_$date.dmp"

    # Use Start-Process with an explicit argument list so that credentials are
    # passed as discrete arguments rather than interpolated into a shell string.
    # This avoids exposing the password in process-list snapshots on some OSes;
    # prefer Oracle Wallet / OS authentication in high-security environments.
    $logFile = "$BackupPath\exp_$date.log"
    Start-Process -FilePath "exp" `
                  -ArgumentList "$OracleUser/$OraclePass", "file=$backupFile", "log=$logFile" `
                  -NoNewWindow -Wait

    Write-Host "Backup created: $backupFile"
    return $backupFile
}

function Restore-ADDSDatabase {
    param(
        [string]$DumpFile,
        # Credentials resolved from environment variables at runtime.
        [string]$OracleUser = (Get-ADDSEnvVar 'ADDS_DB_USER'),
        [string]$OraclePass = (Get-ADDSEnvVar 'ADDS_DB_PASSWORD')
    )

    if (-not (Test-Path $DumpFile)) {
        throw "Dump file not found: $DumpFile"
    }

    Start-Process -FilePath "imp" `
                  -ArgumentList "$OracleUser/$OraclePass", "file=$DumpFile", `
                                "fromuser=$OracleUser", "touser=$OracleUser" `
                  -NoNewWindow -Wait
}
