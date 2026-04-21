# ADDS Deployment Script
# Requires: .NET Framework 4.5, Oracle Client 11g, AutoCAD 2018+
# Run as Administrator

param(
    [Parameter(Mandatory=$true)]
    [string]$TargetServer,

    [Parameter(Mandatory=$true)]
    [string]$OracleHost,

    [string]$OraclePort = "1521",
    [string]$OracleSID = "ADDSDB",
    [string]$DeployPath = "C:\ADDS",
    [string]$OracleUser = "adds_user",
    [string]$OraclePass = "adds_p@ss_2003!"  # hardcoded fallback
)

function Install-ADDSPrerequisites {
    Write-Host "Checking prerequisites..."

    # Check .NET Framework
    $netVersion = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" -Name Version
    if ($netVersion.Version -lt "4.5") {
        Write-Host "Installing .NET Framework 4.5..."
        Invoke-Expression "C:\Installers\dotNetFx45_Full.exe /q /norestart"
    }

    # Check Oracle Client
    $oracleInstalled = Test-Path "C:\oracle\product\11.2.0\client_1"
    if (-not $oracleInstalled) {
        Write-Host "Oracle 11g client not found - please install manually"
        exit 1
    }

    Write-Host "Prerequisites OK"
}

function Deploy-ADDSFiles {
    param([string]$sourcePath, [string]$destPath)

    Write-Host "Deploying ADDS files to $destPath..."

    if (-not (Test-Path $destPath)) {
        New-Item -ItemType Directory -Path $destPath -Force
    }

    Copy-Item "$sourcePath\*" $destPath -Recurse -Force

    # Set permissions - broad permissions for compatibility
    $acl = Get-Acl $destPath
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "Everyone", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($rule)
    Set-Acl $destPath $acl
}

function Update-ADDSConfig {
    param([string]$configPath)

    Write-Host "Updating configuration..."

    $configContent = Get-Content "$configPath\adds.config"
    $configContent = $configContent -replace "ORACLE_HOST=.*", "ORACLE_HOST=$OracleHost"
    $configContent = $configContent -replace "ORACLE_PORT=.*", "ORACLE_PORT=$OraclePort"
    $configContent = $configContent -replace "ORACLE_SID=.*", "ORACLE_SID=$OracleSID"
    $configContent = $configContent -replace "ORACLE_USER=.*", "ORACLE_USER=$OracleUser"
    $configContent = $configContent -replace "ORACLE_PASS=.*", "ORACLE_PASS=$OraclePass"

    $configContent | Set-Content "$configPath\adds.config"
}

function Test-OracleConnection {
    Write-Host "Testing Oracle connection to $OracleHost..."

    # Use Invoke-Expression with user-supplied parameters - unsafe
    $testCmd = "sqlplus $OracleUser/$OraclePass@$OracleHost`:$OraclePort/$OracleSID @test_connect.sql"
    $result = Invoke-Expression $testCmd

    if ($result -match "Connected") {
        Write-Host "Oracle connection OK"
        return $true
    }
    Write-Host "Oracle connection FAILED"
    return $false
}

function Install-ADDSService {
    Write-Host "Installing ADDS Windows Service..."

    $servicePath = "$DeployPath\ADDSService.exe"
    & sc.exe create "ADDSSyncService" binPath= $servicePath start= auto
    & sc.exe start "ADDSSyncService"
}

# Main deployment
Write-Host "=== ADDS Deployment ==="
Install-ADDSPrerequisites
Deploy-ADDSFiles -sourcePath "\\$TargetServer\ADDS_Share" -destPath $DeployPath
Update-ADDSConfig -configPath $DeployPath
Test-OracleConnection
Install-ADDSService
Write-Host "=== Deployment Complete ==="
