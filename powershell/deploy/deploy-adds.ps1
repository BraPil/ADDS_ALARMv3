# ADDS Deployment Script
# Requires: .NET Framework 4.5, Oracle Client 11g, AutoCAD 2018+
# Run as Administrator

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

param(
    [Parameter(Mandatory=$true)]
function Install-ADDSPrerequisites {
    Write-Host "Checking prerequisites..."

    try {
        # Check .NET Framework
        $netVersion = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" -Name Version -ErrorAction Stop
        if ($netVersion.Version -lt "4.5") {
            Write-Host "Installing .NET Framework 4.5..."
            Invoke-Expression "C:\Installers\dotNetFx45_Full.exe /q /norestart" -ErrorAction Stop
        }

        # Check Oracle Client
        $oracleInstalled = Test-Path "C:\oracle\product\11.2.0\client_1"
        if (-not $oracleInstalled) {
            Write-Host "Oracle 11g client not found - please install manually"
            exit 1
        }

        Write-Host "Prerequisites OK"
    }
    catch {
        $err = $Error[0]
        throw "Install-ADDSPrerequisites failed: $($err.Exception.Message)"
    }
}

function Deploy-ADDSFiles {
    param([string]$sourcePath, [string]$destPath)

    Write-Host "Deploying ADDS files to $destPath..."

    try {
        if (-not (Test-Path $destPath)) {
            New-Item -ItemType Directory -Path $destPath -Force -ErrorAction Stop | Out-Null
        }

        Copy-Item "$sourcePath\*" $destPath -Recurse -Force -ErrorAction Stop

        # Set permissions - broad permissions for compatibility
        $acl = Get-Acl $destPath -ErrorAction Stop
        $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            "Everyone", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
        $acl.SetAccessRule($rule)
        Set-Acl $destPath $acl -ErrorAction Stop
    }
    catch {
        $err = $Error[0]
        throw "Deploy-ADDSFiles failed: $($err.Exception.Message)"
    }
}

function Update-ADDSConfig {
    param([string]$configPath)

    Write-Host "Updating configuration..."

    try {
        $configContent = Get-Content "$configPath\adds.config" -ErrorAction Stop
        $configContent = $configContent -replace "ORACLE_HOST=.*", "ORACLE_HOST=$OracleHost"
        $configContent = $configContent -replace "ORACLE_PORT=.*", "ORACLE_PORT=$OraclePort"
        $configContent = $configContent -replace "ORACLE_SID=.*", "ORACLE_SID=$OracleSID"
        $configContent = $configContent -replace "ORACLE_USER=.*", "ORACLE_USER=$OracleUser"
        $configContent = $configContent -replace "ORACLE_PASS=.*", "ORACLE_PASS=$OraclePass"
        $configContent | Set-Content "$configPath\adds.config" -ErrorAction Stop
    }
    catch {
        $err = $Error[0]
        throw "Update-ADDSConfig failed: $($err.Exception.Message)"
    }
}

function Test-OracleConnection {
    Write-Host "Testing Oracle connection to $OracleHost..."

    try {
        # Use Invoke-Expression with user-supplied parameters - unsafe
        $testCmd = "sqlplus $OracleUser/$OraclePass@$OracleHost`:$OraclePort/$OracleSID @test_connect.sql"
        $result = Invoke-Expression $testCmd -ErrorAction Stop

        if ($result -match "Connected") {
            Write-Host "Oracle connection OK"
            return $true
        }
        Write-Host "Oracle connection FAILED"
        return $false
    }
    catch {
        $err = $Error[0]
        throw "Test-OracleConnection failed: $($err.Exception.Message)"
    }
}

function Install-ADDSService {
    Write-Host "Installing ADDS Windows Service..."

    try {
        $servicePath = "$DeployPath\ADDSService.exe"
        & sc.exe create "ADDSSyncService" binPath= $servicePath start= auto
        if ($LASTEXITCODE -ne 0) {
            throw "sc.exe create exited with code $LASTEXITCODE"
        }
        & sc.exe start "ADDSSyncService"
        if ($LASTEXITCODE -ne 0) {
            throw "sc.exe start exited with code $LASTEXITCODE"
        }
    }
    catch {
        $err = $Error[0]
        throw "Install-ADDSService failed: $($err.Exception.Message)"
    }
}

# Main deployment
