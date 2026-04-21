#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

<#
.SYNOPSIS
    ADDS Deployment Script — .NET 10, Oracle 19c, AutoCAD latest
.DESCRIPTION
    Modernized: credentials read from environment variables or Windows Credential Manager,
    Invoke-Expression removed, strict mode enforced, approved verbs only.
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$TargetServer,

    [Parameter(Mandatory=$true)]
    [string]$OracleHost,

    [string]$OraclePort = "1521",
    [string]$OracleSID  = "ADDSDB",
    [string]$DeployPath = "C:\ADDS"
)

# Credentials come from environment — never hardcoded or passed on the command line
function Get-ADDSOracleCredential {
    $user = $env:ADDS_ORACLE_USER
    $pass = $env:ADDS_ORACLE_PASS
    if (-not $user -or -not $pass) {
        # Fall back to Windows Credential Manager
        $cred = Get-StoredCredential -Target 'ADDS_Oracle' -ErrorAction SilentlyContinue
        if ($null -eq $cred) {
            throw "Oracle credentials not found. Set ADDS_ORACLE_USER / ADDS_ORACLE_PASS environment variables or store in Windows Credential Manager as 'ADDS_Oracle'."
        }
        return $cred
    }
    $securePass = ConvertTo-SecureString $pass -AsPlainText -Force
    return New-Object System.Management.Automation.PSCredential($user, $securePass)
}

function Install-ADDSPrerequisites {
    Write-Information "Checking prerequisites..." -InformationAction Continue

    # .NET 10 runtime check
    $dotnetVersion = & dotnet --version 2>$null
    if ($dotnetVersion -notmatch '^10\.') {
        throw ".NET 10 runtime not found (found: $dotnetVersion). Install from https://dot.net."
    }

    # Oracle 19c client check
    if (-not (Test-Path 'C:\oracle\product\19\client_1')) {
        throw "Oracle 19c client not found. Install Oracle Managed Data Access Client 19c."
    }

    Write-Information "Prerequisites OK (.NET $dotnetVersion, Oracle 19c client present)" -InformationAction Continue
}

function Copy-ADDSFiles {
    param(
        [string]$SourcePath,
        [string]$DestPath
    )

    Write-Information "Deploying ADDS files to $DestPath..." -InformationAction Continue

    if (-not (Test-Path $DestPath)) {
        New-Item -ItemType Directory -Path $DestPath -Force | Out-Null
    }

    Copy-Item -Path "$SourcePath\*" -Destination $DestPath -Recurse -Force

    # Restrict to ADDS service account only — not Everyone/FullControl
    $acl = Get-Acl $DestPath
    $acl.SetAccessRuleProtection($true, $false)
    $serviceAccount = 'NT SERVICE\ADDSSyncService'
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        $serviceAccount, 'ReadAndExecute', 'ContainerInherit,ObjectInherit', 'None', 'Allow')
    $acl.AddAccessRule($rule)
    Set-Acl -Path $DestPath -AclObject $acl
}

function Update-ADDSConfig {
    param([string]$ConfigPath)

    Write-Information "Updating configuration..." -InformationAction Continue

    $configFile = Join-Path $ConfigPath 'adds.config'
    $content = Get-Content $configFile -Raw

    $content = $content -replace 'ORACLE_HOST=.*', "ORACLE_HOST=$OracleHost"
    $content = $content -replace 'ORACLE_PORT=.*', "ORACLE_PORT=$OraclePort"
    $content = $content -replace 'ORACLE_SID=.*',  "ORACLE_SID=$OracleSID"
    # User/password are NOT written to config — loaded from environment at runtime

    Set-Content -Path $configFile -Value $content -Encoding UTF8
}

function Test-OracleConnection {
    param([System.Management.Automation.PSCredential]$Credential)

    Write-Information "Testing Oracle connection to $OracleHost..." -InformationAction Continue

    # Use .NET ODP.NET managed driver — no shell injection risk
    Add-Type -Path 'C:\oracle\product\19\client_1\odp.net\managed\common\Oracle.ManagedDataAccess.dll'

    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Credential.Password)
    try {
        $plainPass = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
        $connStr = "Data Source=$OracleHost`:$OraclePort/$OracleSID;User Id=$($Credential.UserName);Password=$plainPass;"
        $conn = New-Object Oracle.ManagedDataAccess.Client.OracleConnection($connStr)
        $conn.Open()
        $conn.Close()
        Write-Information "Oracle connection OK" -InformationAction Continue
        return $true
    }
    catch {
        Write-Warning "Oracle connection failed: $_"
        return $false
    }
    finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Install-ADDSService {
    Write-Information "Installing ADDS Windows Service..." -InformationAction Continue

    $servicePath = Join-Path $DeployPath 'ADDSService.exe'
    if (-not (Test-Path $servicePath)) {
        throw "Service executable not found: $servicePath"
    }

    $existing = Get-Service -Name 'ADDSSyncService' -ErrorAction SilentlyContinue
    if ($existing) {
        Stop-Service -Name 'ADDSSyncService' -Force -ErrorAction SilentlyContinue
        & sc.exe delete 'ADDSSyncService' | Out-Null
    }

    & sc.exe create 'ADDSSyncService' binPath= $servicePath start= auto obj= 'NT SERVICE\ADDSSyncService' | Out-Null
    Start-Service -Name 'ADDSSyncService'
    Write-Information "ADDSSyncService started." -InformationAction Continue
}

# ── Main ─────────────────────────────────────────────────────────────────────

Write-Information "=== ADDS Deployment ===" -InformationAction Continue

$credential = Get-ADDSOracleCredential
Install-ADDSPrerequisites
Copy-ADDSFiles -SourcePath "\\$TargetServer\ADDS_Share" -DestPath $DeployPath
Update-ADDSConfig -ConfigPath $DeployPath

if (-not (Test-OracleConnection -Credential $credential)) {
    throw "Aborting deployment — Oracle connectivity check failed."
}

Install-ADDSService
Write-Information "=== Deployment Complete ===" -InformationAction Continue
