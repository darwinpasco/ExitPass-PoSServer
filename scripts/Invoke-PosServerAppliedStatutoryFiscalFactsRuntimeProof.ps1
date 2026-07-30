[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $DockerImage = 'postgres:16-alpine',

    [Parameter()]
    [ValidateRange(0, 65535)]
    [int] $HostPort = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptRoot '..')).Path
$suffix = ((Get-Date).ToUniversalTime().ToString('yyyyMMddHHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)).ToLowerInvariant()
$databaseName = "pos_smoke_validation_local_$($suffix.Replace('-', '_'))"
$containerName = "posserver-statutory-runtime-proof-$suffix"
$psqlClientContainerName = "$containerName-psql"
$password = [Guid]::NewGuid().ToString('N')
$oldSmokeUrl = $env:POSSERVER_API_SMOKE_DB_URL
$oldDbUrl = $env:POSSERVER_DB_URL
$started = $false

function Get-FreeTcpPort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        return [int]$listener.LocalEndpoint.Port
    }
    finally {
        $listener.Stop()
    }
}

function Invoke-LoggedCommand {
    param(
        [string] $Name,
        [scriptblock] $Command
    )

    Write-Host "== $Name =="
    & $Command
    if ($global:LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $global:LASTEXITCODE."
    }

    Write-Host "PASS: $Name"
}

try {
    if ($HostPort -eq 0) {
        $HostPort = Get-FreeTcpPort
    }

    Write-Host "POS Server statutory runtime proof"
    Write-Host "Repository: $RepoRoot"
    Write-Host "PostgreSQL image: $DockerImage"
    Write-Host "Disposable container: $containerName"
    Write-Host "Disposable database: $databaseName"
    Write-Host "Host port: $HostPort"

    Invoke-LoggedCommand "start disposable PostgreSQL 16 container" {
        docker run `
            --name $containerName `
            -e "POSTGRES_PASSWORD=$password" `
            -e "POSTGRES_DB=$databaseName" `
            -p "127.0.0.1:$($HostPort):5432" `
            -d $DockerImage | Out-Null
    }
    $started = $true

    Invoke-LoggedCommand "wait for PostgreSQL readiness" {
        $ready = $false
        for ($attempt = 1; $attempt -le 60; $attempt++) {
            $output = docker exec $containerName pg_isready -U postgres -d $databaseName 2>&1
            if ($LASTEXITCODE -eq 0) {
                $ready = $true
                break
            }

            Start-Sleep -Seconds 1
        }

        if (-not $ready) {
            throw "PostgreSQL container did not become ready."
        }
    }

    Invoke-LoggedCommand "record PostgreSQL version" {
        $version = $null
        for ($attempt = 1; $attempt -le 60; $attempt++) {
            $candidate = docker exec $containerName psql -U postgres -d $databaseName -t -A -c "show server_version;" 2>&1
            if ($LASTEXITCODE -eq 0) {
                $version = $candidate
                break
            }

            Start-Sleep -Seconds 1
        }

        if ($null -eq $version) {
            throw "Unable to read PostgreSQL version."
        }

        Write-Host "PostgreSQL version: $($version.Trim())"
    }

    $npgsqlConnectionString = "Host=127.0.0.1;Port=$HostPort;Database=$databaseName;Username=postgres;Password=$password;Include Error Detail=false"
    $dockerPsqlConnectionString = "postgresql://postgres:$password@host.docker.internal:$HostPort/$databaseName"
    $env:POSSERVER_API_SMOKE_DB_URL = $npgsqlConnectionString
    $env:POSSERVER_DB_URL = $dockerPsqlConnectionString

    Invoke-LoggedCommand "POS database static validation" {
        powershell -NoProfile -ExecutionPolicy Bypass `
            -File .\db\scripts\Invoke-PosDbChecks.ps1 `
            -Mode Static `
            -EvidenceDir .\db\validation\evidence\statutory-runtime-proof-static
    }

    Invoke-LoggedCommand "POS database Docker-backed validation" {
        powershell -NoProfile -ExecutionPolicy Bypass `
            -File .\db\scripts\Invoke-PosDbChecks.ps1 `
            -Mode All `
            -UseDockerPsql `
            -ConnectionString $env:POSSERVER_DB_URL `
            -DatabaseName $databaseName `
            -DockerContainerName $psqlClientContainerName `
            -EvidenceDir .\db\validation\evidence\statutory-runtime-proof-all
    }

    Invoke-LoggedCommand "ordinary fiscal issuance API/database proof" {
        dotnet test `
            .\tests\ExitPass.PosServer.Api.IntegrationTests\ExitPass.PosServer.Api.IntegrationTests.csproj `
            -c Release `
            --no-build `
            --filter "FullyQualifiedName~PostFiscalDocumentWritesCompletePersistenceShell"
    }

    Invoke-LoggedCommand "statutory fiscal issuance API/database proof" {
        dotnet test `
            .\tests\ExitPass.PosServer.Api.IntegrationTests\ExitPass.PosServer.Api.IntegrationTests.csproj `
            -c Release `
            --no-build `
            --filter "FullyQualifiedName~PostAppliedStatutoryFiscalDocumentPersistsReadbackAndPresentationSnapshot"
    }

    Invoke-LoggedCommand "late persistence rollback API/database proof" {
        dotnet test `
            .\tests\ExitPass.PosServer.Api.IntegrationTests\ExitPass.PosServer.Api.IntegrationTests.csproj `
            -c Release `
            --no-build `
            --filter "FullyQualifiedName~LatePersistenceFailureRollsBackFiscalDocumentShell"
    }

    Write-Host "PASS: applied statutory fiscal facts runtime proof completed."
}
finally {
    $env:POSSERVER_API_SMOKE_DB_URL = $oldSmokeUrl
    $env:POSSERVER_DB_URL = $oldDbUrl

    if ($started) {
        Write-Host "Cleaning up disposable container: $containerName"
        docker rm -f $containerName | Out-Null
        Write-Host "PASS: disposable PostgreSQL cleanup completed."
    }
}
