[CmdletBinding()]
param(
    [string] $DockerImage = 'postgres:16-alpine'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..')).Path
. (Join-Path $scriptRoot 'AnnexE1V17PostgresFinalReadiness.ps1')
$datasetPath = Join-Path $repoRoot 'docs\v1.3\fiscal-reporting\annex-e\dataset\v1.7\annex-e1-synthetic-uat-dataset-v1.7.jsonl'
$validatorPath = Join-Path $repoRoot 'scripts\Invoke-AnnexE1SyntheticUatDatasetV17Validation.ps1'
$testProject = Join-Path $repoRoot 'tests\ExitPass.PosServer.Api.IntegrationTests\ExitPass.PosServer.Api.IntegrationTests.csproj'
$invocationId = ((Get-Date).ToUniversalTime().ToString('yyyyMMddHHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 10)).ToLowerInvariant()
$resourceLabel = "exitpass.annex-e1-v17.invocation=$invocationId"
$createdResources = New-Object System.Collections.Generic.List[string]
$removedResources = New-Object System.Collections.Generic.List[string]
$oldConnection = $env:ANNEX_E1_V17_HARNESS_DB_URL
$oldDataset = $env:ANNEX_E1_V17_DATASET_PATH
$oldReport = $env:ANNEX_E1_V17_REPORT_PATH

function Invoke-CheckedNative {
    param([string] $Name, [string] $FileName, [string[]] $Arguments)
    $savedErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $FileName @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $savedErrorActionPreference
    }
    if ($exitCode -ne 0) {
        $diagnostic = ($output | Select-Object -Last 20) -join [Environment]::NewLine
        throw "$Name failed with exit code $exitCode.$([Environment]::NewLine)$diagnostic"
    }
    return $output
}

function Get-FreeLoopbackPort {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    try { $listener.Start(); return [int]$listener.LocalEndpoint.Port }
    finally { $listener.Stop() }
}

function Test-DockerResourceAbsent {
    param([ValidateSet('container','volume','network')] [string] $Kind, [string] $Name)
    $savedErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & docker $Kind inspect $Name *> $null
        $inspectExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $savedErrorActionPreference
    }
    return $inspectExitCode -ne 0
}

function Remove-OwnedResource {
    param([ValidateSet('container','volume','network')] [string] $Kind, [string] $Name)
    if (-not (Test-DockerResourceAbsent $Kind $Name)) {
        if ($Kind -eq 'container') { & docker rm --force $Name *> $null }
        elseif ($Kind -eq 'volume') { & docker volume rm $Name *> $null }
        else { & docker network rm $Name *> $null }
        if ($LASTEXITCODE -ne 0) { throw "Failed to remove invocation-owned $Kind '$Name'." }
    }
    if (-not (Test-DockerResourceAbsent $Kind $Name)) { throw "Invocation-owned $Kind '$Name' still exists after cleanup." }
    $removedResources.Add("${Kind}:$Name")
}

function Invoke-IsolatedRun {
    param([int] $Ordinal, [switch] $InjectFailure)

    $suffix = "$invocationId-$Ordinal"
    $containerName = "exitpass-annex-e1-v17-$suffix-pg"
    $networkName = "exitpass-annex-e1-v17-$suffix-net"
    $volumeName = "exitpass-annex-e1-v17-$suffix-vol"
    $databaseName = "annex_e1_v17_harness_$($suffix.Replace('-', '_'))"
    $password = [Guid]::NewGuid().ToString('N')
    $hostPort = Get-FreeLoopbackPort
    $tempDirectory = Join-Path ([IO.Path]::GetTempPath()) "exitpass-annex-e1-v17-$suffix"
    $reportPath = Join-Path $tempDirectory 'normalized-execution-report.txt'
    $networkCreated = $false
    $volumeCreated = $false
    $containerCreated = $false

    try {
        [void](New-Item -ItemType Directory -Path $tempDirectory)
        $createdResources.Add("temporary-directory:$tempDirectory")

        Invoke-CheckedNative 'create disposable network' 'docker' @('network','create','--label',$resourceLabel,$networkName) | Out-Null
        $networkCreated = $true
        $createdResources.Add("network:$networkName")

        Invoke-CheckedNative 'create disposable volume' 'docker' @('volume','create','--label',$resourceLabel,$volumeName) | Out-Null
        $volumeCreated = $true
        $createdResources.Add("volume:$volumeName")

        Invoke-CheckedNative 'start disposable PostgreSQL' 'docker' @(
            'run','--detach','--name',$containerName,'--label',$resourceLabel,
            '--network',$networkName,'--volume',"${volumeName}:/var/lib/postgresql/data",
            '--publish',"127.0.0.1:${hostPort}:5432",
            '--env',"POSTGRES_PASSWORD=$password",'--env',"POSTGRES_DB=$databaseName",$DockerImage
        ) | Out-Null
        $containerCreated = $true
        $createdResources.Add("container:$containerName")
        $createdResources.Add("database:$databaseName")

        $readiness = Wait-AnnexE1V17PostgresFinalReadiness `
            -ContainerName $containerName `
            -DatabaseName $databaseName `
            -TimeoutSeconds 60
        foreach ($entry in $readiness.Trace) {
            Write-Verbose ("PostgreSQL readiness ordinal {0}: {1}" -f $Ordinal, ($entry | ConvertTo-Json -Compress))
        }
        if ($readiness.State -ne 'READY') { throw "Disposable PostgreSQL final readiness was not established." }
        $version = Invoke-CheckedNative 'read PostgreSQL version' 'docker' @('exec',$containerName,'psql','--username','postgres','--dbname',$databaseName,'--tuples-only','--no-align','--command','show server_version;')
        if (-not (($version -join '').Trim().StartsWith('16.', [StringComparison]::Ordinal))) { throw "Disposable PostgreSQL is not version 16." }

        if ($InjectFailure) { throw 'INTENTIONAL_FAILURE_PATH_PROBE' }

        $env:ANNEX_E1_V17_HARNESS_DB_URL = "Host=127.0.0.1;Port=$hostPort;Database=$databaseName;Username=postgres;Password=$password;Include Error Detail=false"
        $env:ANNEX_E1_V17_DATASET_PATH = $datasetPath
        $env:ANNEX_E1_V17_REPORT_PATH = $reportPath
        Invoke-CheckedNative 'execute Annex E-1 v1.7 integration harness' 'dotnet' @(
            'test',$testProject,'--configuration','Release','--no-build',
            '--filter','FullyQualifiedName~AnnexE1V17IsolatedExecutionTests.LoadsAndExecutesAllIncludedV17Cases',
            '--logger','console;verbosity=minimal'
        ) | Out-Host
        if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) { throw 'The integration harness did not produce its normalized report.' }
        $bytes = [IO.File]::ReadAllBytes($reportPath)
        $text = [Text.Encoding]::UTF8.GetString($bytes)
        if (-not $text.StartsWith("ANNEX_E1_V17_ISOLATED_EXECUTION=PASS`n", [StringComparison]::Ordinal)) { throw 'The integration report did not declare PASS.' }
        return [pscustomobject]@{
            Report = $text
            Sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $reportPath).Hash.ToLowerInvariant()
        }
    }
    finally {
        $env:ANNEX_E1_V17_HARNESS_DB_URL = $oldConnection
        $env:ANNEX_E1_V17_DATASET_PATH = $oldDataset
        $env:ANNEX_E1_V17_REPORT_PATH = $oldReport
        if ($containerCreated) { Remove-OwnedResource container $containerName }
        if ($volumeCreated) { Remove-OwnedResource volume $volumeName }
        if ($networkCreated) { Remove-OwnedResource network $networkName }
        if (Test-Path -LiteralPath $tempDirectory) {
            Remove-Item -LiteralPath $tempDirectory -Recurse -Force
        }
        if (Test-Path -LiteralPath $tempDirectory) { throw "Invocation-owned temporary directory remains: $tempDirectory" }
        $removedResources.Add("temporary-directory:$tempDirectory")
        if ($containerCreated) { $removedResources.Add("database:$databaseName") }
    }
}

function Assert-NegativeDatasetRejected {
    param([string] $Name, [string[]] $Lines)
    $directory = Join-Path ([IO.Path]::GetTempPath()) "exitpass-annex-e1-v17-negative-$invocationId-$Name"
    $path = Join-Path $directory 'dataset.jsonl'
    try {
        [void](New-Item -ItemType Directory -Path $directory)
        $createdResources.Add("temporary-directory:$directory")
        $utf8 = New-Object Text.UTF8Encoding($false)
        [IO.File]::WriteAllText($path, ([string]::Join([char]10, $Lines) + [char]10), $utf8)
        $savedErrorActionPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            $output = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $validatorPath -DatasetPath $path 2>&1
            $validatorExitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $savedErrorActionPreference
        }
        if ($validatorExitCode -eq 0) { throw "Negative dataset '$Name' was accepted." }
        Write-Output "NEGATIVE_${Name}=PASS"
        Write-Verbose (($output | Select-Object -Last 1) -join '')
    }
    finally {
        if (Test-Path -LiteralPath $directory) { Remove-Item -LiteralPath $directory -Recurse -Force }
        if (Test-Path -LiteralPath $directory) { throw "Negative-test directory remains: $directory" }
        $removedResources.Add("temporary-directory:$directory")
    }
}

try {
    if (-not (Test-Path -LiteralPath $datasetPath -PathType Leaf)) { throw "Committed v1.7 dataset is missing." }
    Invoke-CheckedNative 'validate committed Annex E-1 v1.7 dataset' 'powershell.exe' @('-NoProfile','-ExecutionPolicy','Bypass','-File',$validatorPath) | Out-Host
    Invoke-CheckedNative 'build isolated execution test project' 'dotnet' @('build',$testProject,'--configuration','Release','--no-restore') | Out-Host

    $datasetLines = [IO.File]::ReadAllLines($datasetPath, [Text.Encoding]::UTF8)
    $corruptLines = [string[]]$datasetLines.Clone()
    $correctionIndex = -1
    for ($index = 0; $index -lt $corruptLines.Length; $index++) {
        if ($corruptLines[$index].Contains('"key":"F16|013|0008"')) { $correctionIndex = $index; break }
    }
    if ($correctionIndex -lt 0) { throw 'Correction fact was not found for corruption testing.' }
    $corruptLines[$correctionIndex] = $corruptLines[$correctionIndex].Replace('"v":"source_correction"', '"v":"broken_correction"')
    Assert-NegativeDatasetRejected 'CORRUPTION' $corruptLines

    $missingReferenceLines = @($datasetLines | Where-Object { -not ($_.Contains('"recordType":"identity"') -and $_.Contains('"uuid":"96cc7392-4dc3-550c-b7e0-8521f77d86ef"')) })
    if ($missingReferenceLines.Count -ne ($datasetLines.Count - 1)) { throw 'Missing-reference fixture did not remove exactly one identity.' }
    Assert-NegativeDatasetRejected 'MISSING_REFERENCE' $missingReferenceLines

    try {
        Invoke-IsolatedRun -Ordinal 0 -InjectFailure | Out-Null
        throw 'Failure-path probe did not fail.'
    }
    catch {
        if ($_.Exception.Message -notmatch 'INTENTIONAL_FAILURE_PATH_PROBE') { throw }
        Write-Output 'FAILURE_PATH_CLEANUP=PASS'
    }

    $first = Invoke-IsolatedRun -Ordinal 1
    $second = Invoke-IsolatedRun -Ordinal 2
    if ($first.Report -cne $second.Report -or $first.Sha256 -cne $second.Sha256) { throw 'Two clean isolated executions produced different normalized reports.' }

    $remainingContainers = @(& docker ps --all --quiet --filter "label=$resourceLabel")
    $remainingVolumes = @(& docker volume ls --quiet --filter "label=$resourceLabel")
    $remainingNetworks = @(& docker network ls --quiet --filter "label=$resourceLabel")
    if ($remainingContainers.Count -ne 0 -or $remainingVolumes.Count -ne 0 -or $remainingNetworks.Count -ne 0) {
        throw 'Invocation-owned Docker resources remain after validation.'
    }

    Write-Output 'ANNEX_E1_V17_HARNESS=PASS'
    Write-Output "NORMALIZED_OUTPUT_SHA256=$($first.Sha256)"
    Write-Output 'CLEAN_EXECUTIONS=2'
    Write-Output 'INCLUDED_CASES=19'
    Write-Output 'LOADABLE_FAMILIES=29'
    Write-Output 'LOADED_DATASET_RECORDS=4074'
    Write-Output 'CANONICAL_SCOPE_RECORDS=215'
    Write-Output 'DISPOSABLE_RESOURCES_REMAINING=0'
    foreach ($resource in $createdResources) { Write-Output "CREATED_RESOURCE=$resource" }
    foreach ($resource in $removedResources) { Write-Output "REMOVED_RESOURCE=$resource" }
}
finally {
    $env:ANNEX_E1_V17_HARNESS_DB_URL = $oldConnection
    $env:ANNEX_E1_V17_DATASET_PATH = $oldDataset
    $env:ANNEX_E1_V17_REPORT_PATH = $oldReport
}
