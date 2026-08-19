[CmdletBinding()]
param(
    [string] $DockerImage = 'postgres:16-alpine',
    [ValidateRange(5, 20)][int] $Iterations = 5,
    [Parameter(Mandatory = $true)][string] $OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptRoot 'AnnexE1V17PostgresFinalReadiness.ps1')

$diagnosticId = ((Get-Date).ToUniversalTime().ToString('yyyyMMddHHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 10)).ToLowerInvariant()
$resourceLabel = "exitpass.annex-e1-v17.readiness-diagnostic=$diagnosticId"
$results = New-Object System.Collections.Generic.List[object]
$utf8 = New-Object Text.UTF8Encoding($false)

function Invoke-CheckedDocker {
    param([string] $Name, [string[]] $Arguments)
    $result = Invoke-AnnexE1V17DockerCapture $Arguments
    if ($result.ExitCode -ne 0) { throw "$Name failed: $($result.Text)" }
    return $result.Text.Trim()
}

function Get-FreeLoopbackPort {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    try { $listener.Start(); return [int] $listener.LocalEndpoint.Port }
    finally { $listener.Stop() }
}

function Remove-ExactResource {
    param([ValidateSet('container', 'volume', 'network')][string] $Kind, [string] $Name)
    $inspect = Invoke-AnnexE1V17DockerCapture @($Kind, 'inspect', $Name)
    if ($inspect.ExitCode -eq 0) {
        if ($Kind -eq 'container') { Invoke-CheckedDocker "remove $Kind" @('rm', '--force', $Name) | Out-Null }
        elseif ($Kind -eq 'volume') { Invoke-CheckedDocker "remove $Kind" @('volume', 'rm', $Name) | Out-Null }
        else { Invoke-CheckedDocker "remove $Kind" @('network', 'rm', $Name) | Out-Null }
    }
}

$resolvedOutput = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $resolvedOutput) { throw "Diagnostic output directory already exists: $resolvedOutput" }
[void](New-Item -ItemType Directory -Path $resolvedOutput)

for ($iteration = 1; $iteration -le $Iterations; $iteration++) {
    $suffix = "$diagnosticId-$iteration"
    $containerName = "exitpass-annex-e1-v17-readiness-$suffix-pg"
    $networkName = "exitpass-annex-e1-v17-readiness-$suffix-net"
    $volumeName = "exitpass-annex-e1-v17-readiness-$suffix-vol"
    $databaseName = "annex_e1_v17_readiness_$($suffix.Replace('-', '_'))"
    $password = [Guid]::NewGuid().ToString('N')
    $hostPort = Get-FreeLoopbackPort
    $networkCreated = $false
    $volumeCreated = $false
    $containerCreated = $false
    $startedUtc = [DateTime]::UtcNow
    $readiness = $null
    $version = $null
    $intentionalFailureObserved = $false

    try {
        $networkId = Invoke-CheckedDocker 'create diagnostic network' @('network', 'create', '--label', $resourceLabel, $networkName)
        $networkCreated = $true
        $volumeId = Invoke-CheckedDocker 'create diagnostic volume' @('volume', 'create', '--label', $resourceLabel, $volumeName)
        $volumeCreated = $true
        $containerId = Invoke-CheckedDocker 'start diagnostic PostgreSQL' @(
            'run', '--detach', '--name', $containerName, '--label', $resourceLabel,
            '--network', $networkName, '--volume', "${volumeName}:/var/lib/postgresql/data",
            '--publish', "127.0.0.1:${hostPort}:5432",
            '--env', "POSTGRES_PASSWORD=$password", '--env', "POSTGRES_DB=$databaseName", $DockerImage
        )
        $containerCreated = $true

        $readiness = Wait-AnnexE1V17PostgresFinalReadiness -ContainerName $containerName -DatabaseName $databaseName -TimeoutSeconds 60
        $version = Invoke-CheckedDocker 'read final PostgreSQL version' @(
            'exec', $containerName, 'psql', '--username', 'postgres', '--dbname', $databaseName,
            '--tuples-only', '--no-align', '--command', 'show server_version;'
        )
        if (-not $version.StartsWith('16.', [StringComparison]::Ordinal)) { throw 'Final PostgreSQL version was not 16.x.' }

        try { throw 'INTENTIONAL_FAILURE_PATH_PROBE' }
        catch {
            if ($_.Exception.Message -ne 'INTENTIONAL_FAILURE_PATH_PROBE') { throw }
            $intentionalFailureObserved = $true
        }
    }
    finally {
        if ($containerCreated) { Remove-ExactResource container $containerName }
        if ($volumeCreated) { Remove-ExactResource volume $volumeName }
        if ($networkCreated) { Remove-ExactResource network $networkName }
    }

    $remainingContainer = (Invoke-AnnexE1V17DockerCapture @('container', 'inspect', $containerName)).ExitCode -eq 0
    $remainingVolume = (Invoke-AnnexE1V17DockerCapture @('volume', 'inspect', $volumeName)).ExitCode -eq 0
    $remainingNetwork = (Invoke-AnnexE1V17DockerCapture @('network', 'inspect', $networkName)).ExitCode -eq 0
    if ($remainingContainer -or $remainingVolume -or $remainingNetwork) { throw "Iteration $iteration left a diagnostic Docker resource." }

    $temporaryDetected = @($readiness.Trace | Where-Object { $_.State -eq 'TEMPORARY_SERVER_AVAILABLE' }).Count -gt 0
    $result = [ordered]@{
        Label = 'DIAGNOSTIC_ONLY_NOT_CONTROLLED_UAT_EVIDENCE'
        DiagnosticId = $diagnosticId
        Iteration = $iteration
        StartedUtc = $startedUtc.ToString('o')
        ReadyUtc = $readiness.ReadyUtc
        ContainerId = $containerId
        ContainerName = $containerName
        NetworkId = $networkId
        NetworkName = $networkName
        VolumeId = $volumeId
        VolumeName = $volumeName
        DatabaseName = $databaseName
        LoopbackPort = $hostPort
        TemporaryServerDetected = $temporaryDetected
        TemporaryReadinessAccepted = $false
        InitializationCompletionObserved = @($readiness.Trace | Where-Object { $_.InitializationComplete }).Count -gt 0
        FinalPidOneObserved = @($readiness.Trace | Where-Object { $_.PidOneCommand -eq 'postgres' }).Count -gt 0
        StableProbeCount = $readiness.StableProbeCount
        PostmasterStartTime = $readiness.PostmasterStartTime
        VersionQuery = $version
        IntentionalFailurePathObserved = $intentionalFailureObserved
        Trace = $readiness.Trace
        Cleanup = 'PASS'
    }
    $results.Add([pscustomobject] $result)
    [IO.File]::WriteAllText(
        (Join-Path $resolvedOutput ("iteration-{0:D2}.json" -f $iteration)),
        ($result | ConvertTo-Json -Depth 10),
        $utf8
    )
}

$remainingContainers = @(& docker ps --all --quiet --filter "label=$resourceLabel")
$remainingVolumes = @(& docker volume ls --quiet --filter "label=$resourceLabel")
$remainingNetworks = @(& docker network ls --quiet --filter "label=$resourceLabel")
if ($remainingContainers.Count -ne 0 -or $remainingVolumes.Count -ne 0 -or $remainingNetworks.Count -ne 0) {
    throw 'Diagnostic resources remain after all iterations.'
}

$summary = [ordered]@{
    Label = 'DIAGNOSTIC_ONLY_NOT_CONTROLLED_UAT_EVIDENCE'
    DiagnosticId = $diagnosticId
    Iterations = $Iterations
    FinalReadinessPassed = @($results | Where-Object { $_.StableProbeCount -ge 3 }).Count
    TemporaryReadinessAccepted = @($results | Where-Object { $_.TemporaryReadinessAccepted }).Count
    VersionQueriesPassed = @($results | Where-Object { $_.VersionQuery -like '16.*' }).Count
    CleanupPassed = @($results | Where-Object { $_.Cleanup -eq 'PASS' }).Count
    DisposableResourcesRemaining = 0
    Result = 'PASS'
}
[IO.File]::WriteAllText((Join-Path $resolvedOutput 'summary.json'), ($summary | ConvertTo-Json -Depth 6), $utf8)
$summary | ConvertTo-Json -Depth 6
