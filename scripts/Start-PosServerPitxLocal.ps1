[CmdletBinding()]
param(
    [switch] $SmokeTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$databaseContainer = 'exitpass-pos-ist-persistent-db'
$databaseName = 'exitpass_pos_ist'
$databaseUser = 'exitpass_ist'
$databaseVolume = 'exitpass-pos-ist-persistent-data'
$networkName = 'exitpass-ist-persistent'
$containerName = 'exitpass-pos-server-pitx-local'
$sitePosServerId = '3a138565-1b88-55f8-c83d-5380db6edccc'
$httpsUrl = 'https://localhost:56066'
$httpUrl = 'http://127.0.0.1:56067'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $repoRoot 'src\ExitPass.PosServer.Api\ExitPass.PosServer.Api.csproj'
$dockerfilePath = Join-Path $repoRoot 'src\ExitPass.PosServer.Api\Dockerfile'
$privateRoot = if ([string]::IsNullOrWhiteSpace($env:EXITPASS_PERSISTENT_IST_ROOT)) {
    'D:\SourceCodes\ExitPass.local\persistent-ist'
}
else {
    $env:EXITPASS_PERSISTENT_IST_ROOT
}
$environmentFile = Join-Path $privateRoot 'runtime\restart-41-business\pos.env'
$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) "exitpass-pos-server-pitx-local-$PID"
$certificatePath = Join-Path $temporaryRoot 'exitpass-pos-local.pfx'
$containerStarted = $false

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory)][string] $FilePath,
        [Parameter(Mandatory)][string[]] $Arguments,
        [Parameter(Mandatory)][string] $FailureMessage
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw $FailureMessage
    }
}

function Get-PrivateEnvironmentValue {
    param(
        [Parameter(Mandatory)][string] $Path,
        [Parameter(Mandatory)][string] $Name
    )

    $prefix = "$Name="
    $line = Get-Content -LiteralPath $Path |
        Where-Object { $_.StartsWith($prefix, [StringComparison]::Ordinal) } |
        Select-Object -Last 1
    if ($null -eq $line) {
        return $null
    }

    return $line.Substring($prefix.Length)
}

function New-RandomSecret {
    $bytes = [byte[]]::new(32)
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
    }
    finally {
        $generator.Dispose()
    }

    return [Convert]::ToBase64String($bytes)
}

function Wait-ForHealth {
    param(
        [Parameter(Mandatory)][string] $Path,
        [int] $Attempts = 60
    )

    $uri = "$httpUrl$Path"
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri $uri -TimeoutSec 2
            if ($response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            if ($attempt -eq $Attempts) {
                throw "POS Server did not become healthy at $uri. Inspect with: docker logs $containerName"
            }
        }

        Start-Sleep -Seconds 1
    }
}

if (-not (Get-Command docker.exe -ErrorAction SilentlyContinue)) {
    throw 'Docker Desktop is required for the persistent PITX POS database network.'
}
if (-not (Get-Command dotnet.exe -ErrorAction SilentlyContinue)) {
    throw 'The .NET 8 SDK is required to build and export the local HTTPS certificate.'
}
if (-not (Test-Path -LiteralPath $projectPath) -or -not (Test-Path -LiteralPath $dockerfilePath)) {
    throw 'Run this launcher from a complete ExitPass-PoSServer source checkout.'
}
if (-not (Test-Path -LiteralPath $environmentFile)) {
    throw "Persistent PITX POS configuration was not found at $environmentFile."
}

$databaseConnection = Get-PrivateEnvironmentValue $environmentFile 'ConnectionStrings__PosServer'
if ([string]::IsNullOrWhiteSpace($databaseConnection) -or
    $databaseConnection -notmatch '(?i)(^|;)\s*Host=exitpass-pos-ist-persistent-db\s*(;|$)' -or
    $databaseConnection -notmatch '(?i)(^|;)\s*Database=exitpass_pos_ist\s*(;|$)') {
    throw 'The private POS environment does not target the authoritative persistent PITX POS IST database.'
}

$databaseInspectJson = (& docker.exe inspect $databaseContainer 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($databaseInspectJson)) {
    throw "The persistent POS database is not running. Start container '$databaseContainer' before launching POS Server."
}
$databaseInspect = $databaseInspectJson | ConvertFrom-Json
if (-not $databaseInspect[0].State.Running) {
    throw "The persistent POS database is not running. Start container '$databaseContainer' before launching POS Server."
}
$mountedVolume = $databaseInspect[0].Mounts |
    Where-Object { $_.Destination -eq '/var/lib/postgresql/data' } |
    Select-Object -ExpandProperty Name -First 1
if ($mountedVolume -ne $databaseVolume) {
    throw "Container '$databaseContainer' is not attached to expected volume '$databaseVolume'."
}
Invoke-CheckedCommand docker.exe @(
    'exec', $databaseContainer, 'pg_isready', '-U', $databaseUser, '-d', $databaseName
) "The persistent POS database is unavailable. Verify '$databaseContainer' and retry."

$networkInspectJson = (& docker.exe network inspect $networkName 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($networkInspectJson)) {
    throw "Docker network '$networkName' is unavailable. Start the persistent IST resources first."
}
$networkInspect = $networkInspectJson | ConvertFrom-Json
if ($networkInspect[0].Name -ne $networkName) {
    throw "Docker network '$networkName' is unavailable. Start the persistent IST resources first."
}
$existingContainer = (& docker.exe ps -a --filter "name=^/$containerName$" --format '{{.Names}}')
if (-not [string]::IsNullOrWhiteSpace($existingContainer)) {
    throw "Container '$containerName' already exists. Stop its launcher or remove the stale local-runtime container before retrying."
}

$sourceSha = (& git.exe -C $repoRoot rev-parse --short=12 HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($sourceSha)) {
    throw 'Unable to determine the POS Server source revision.'
}
$imageName = "exitpass-pos-server-local:$sourceSha"

[void](New-Item -ItemType Directory -Path $temporaryRoot -Force)
$certificatePassword = New-RandomSecret

try {
    Invoke-CheckedCommand dotnet.exe @(
        'dev-certs', 'https', '--export-path', $certificatePath, '--password', $certificatePassword
    ) 'Unable to export the local ASP.NET Core HTTPS development certificate.'

    Invoke-CheckedCommand docker.exe @(
        'build', '--file', $dockerfilePath, '--tag', $imageName, $repoRoot
    ) 'POS Server image build failed.'

    $containerId = (& docker.exe run --rm --detach `
        --name $containerName `
        --network $networkName `
        --network-alias 'exitpass-r41-pos-server' `
        --network-alias 'pitx-pos-server' `
        --env-file $environmentFile `
        --env 'ASPNETCORE_URLS=http://+:8080;https://+:8443' `
        --env 'ASPNETCORE_Kestrel__Certificates__Default__Path=/https/exitpass-pos-local.pfx' `
        --env "ASPNETCORE_Kestrel__Certificates__Default__Password=$certificatePassword" `
        --publish '127.0.0.1:56067:8080' `
        --publish '127.0.0.1:56066:8443' `
        --mount "type=bind,source=$certificatePath,target=/https/exitpass-pos-local.pfx,readonly" `
        --label 'com.exitpass.local-runtime=pitx-pos-server' `
        $imageName).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($containerId)) {
        throw 'POS Server container failed to start. Confirm ports 56066 and 56067 are available.'
    }
    $containerStarted = $true

    Wait-ForHealth '/health/live'
    Wait-ForHealth '/health/ready'

    Write-Host "POS Server is ready for PITX Level 3."
    Write-Host "Site POS Server ID: $sitePosServerId"
    Write-Host "HTTPS: $httpsUrl"
    Write-Host "HTTP:  $httpUrl"
    Write-Host "Database: $databaseContainer/$databaseName (volume $databaseVolume)"

    if (-not $SmokeTest) {
        Write-Host 'Press Ctrl+C to stop the local POS Server.'
        & docker.exe logs --follow $containerName
    }
}
finally {
    if ($containerStarted) {
        & docker.exe stop --time 10 $containerName 2>$null | Out-Null
    }
    if (Test-Path -LiteralPath $temporaryRoot) {
        $resolvedTemporaryRoot = (Resolve-Path -LiteralPath $temporaryRoot).Path
        $systemTemporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
        if (-not $resolvedTemporaryRoot.StartsWith(
                "$systemTemporaryRoot\exitpass-pos-server-pitx-local-",
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove unexpected temporary path: $resolvedTemporaryRoot"
        }
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
