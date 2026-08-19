Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
. (Join-Path $repoRoot 'scripts\AnnexE1V17PostgresFinalReadiness.ps1')

$script:passed = 0

function Assert-Condition {
    param([bool] $Condition, [string] $Message)
    if (-not $Condition) { throw $Message }
}

function New-Observation {
    param(
        [string] $Status = 'running',
        [bool] $Running = $true,
        [bool] $Restarting = $false,
        [int] $RestartCount = 0,
        [bool] $PgIsReady = $false,
        [bool] $InitializationComplete = $false,
        [string] $PidOneCommand = 'docker-entrypoint.sh',
        [bool] $SqlSucceeded = $false,
        [string] $SqlError = '',
        [string] $ServerVersion = '',
        [string] $PostmasterStartTime = ''
    )
    [pscustomobject]@{
        DockerError = $null; Exists = $true; Status = $Status; Running = $Running
        Restarting = $Restarting; RestartCount = $RestartCount; PgIsReady = $PgIsReady
        InitializationComplete = $InitializationComplete; PidOneCommand = $PidOneCommand
        SqlSucceeded = $SqlSucceeded; SqlError = $SqlError; ServerVersion = $ServerVersion
        PostmasterStartTime = $PostmasterStartTime
    }
}

function New-SequenceProvider {
    param([object[]] $Observations)
    $sequenceState = [pscustomobject]@{ Index = 0 }
    return {
        param($ContainerName, $DatabaseName)
        $selected = if ($sequenceState.Index -lt $Observations.Count) { $Observations[$sequenceState.Index] } else { $Observations[-1] }
        $sequenceState.Index++
        return $selected
    }.GetNewClosure()
}

function New-AdvancingClock {
    param([int] $StepMilliseconds = 100)
    $clockState = [pscustomobject]@{
        Now = [DateTime]::Parse('2026-08-19T00:00:00Z').ToUniversalTime()
    }
    return {
        $current = $clockState.Now
        $clockState.Now = $clockState.Now.AddMilliseconds($StepMilliseconds)
        return $current
    }.GetNewClosure()
}

function Invoke-Test {
    param([string] $Name, [scriptblock] $Body)
    & $Body
    $script:passed++
    Write-Output "TEST_PASS=$Name"
}

function Invoke-Readiness {
    param([object[]] $Observations, [int] $TimeoutSeconds = 10)
    Wait-AnnexE1V17PostgresFinalReadiness `
        -ContainerName 'test-pg' -DatabaseName 'annex_test' `
        -ProbeProvider (New-SequenceProvider $Observations) `
        -Delay { param($Milliseconds) } `
        -UtcNow (New-AdvancingClock) `
        -TimeoutSeconds $TimeoutSeconds
}

$starting = New-Observation -Status 'created' -Running $false
$temporary = New-Observation -PgIsReady $true
$temporaryStopped = New-Observation -PgIsReady $false
$finalStarting = New-Observation -InitializationComplete $true -PidOneCommand 'docker-entrypoint.sh'
$stableA = New-Observation -PgIsReady $true -InitializationComplete $true -PidOneCommand 'postgres' -SqlSucceeded $true -ServerVersion '16.10' -PostmasterStartTime '2026-08-19 00:00:01+00'
$stableB = New-Observation -PgIsReady $true -InitializationComplete $true -PidOneCommand 'postgres' -SqlSucceeded $true -ServerVersion '16.10' -PostmasterStartTime '2026-08-19 00:00:02+00'
$transientSqlFailure = New-Observation -PgIsReady $true -InitializationComplete $true -PidOneCommand 'postgres' -SqlError 'server is starting up'

Invoke-Test 'temporary-server-pg-isready-is-not-final-readiness' {
    $result = Invoke-Readiness @($temporary, $temporaryStopped, $finalStarting, $stableA, $stableA, $stableA)
    Assert-Condition ($result.Trace[0].State -eq 'TEMPORARY_SERVER_AVAILABLE') 'Temporary server was not identified.'
    Assert-Condition ($result.Trace[0].ConsecutiveStableProbes -eq 0) 'Temporary readiness advanced stability.'
}

Invoke-Test 'temporary-server-shutdown-resets-readiness' {
    $result = Invoke-Readiness @($temporary, $temporaryStopped, $stableA, $stableA, $stableA)
    Assert-Condition ($result.Trace[1].State -eq 'INITIALIZING') 'Temporary shutdown was not observed.'
}

Invoke-Test 'final-server-start-is-distinct' {
    $result = Invoke-Readiness @($finalStarting, $stableA, $stableA, $stableA)
    Assert-Condition ($result.Trace[0].State -eq 'FINAL_SERVER_STARTING') 'Final-server startup was not distinct.'
}

Invoke-Test 'legacy-first-pg-isready-would-accept-too-early' {
    $sequence = @($temporary, $temporaryStopped, $finalStarting, $stableA, $stableA, $stableA)
    $legacyAcceptanceIndex = 0
    $result = Invoke-Readiness $sequence
    $readyTraceIndex = $result.Trace.Count - 1
    Assert-Condition ($legacyAcceptanceIndex -lt $readyTraceIndex) 'Test did not prove legacy early acceptance.'
    Assert-Condition ($result.Trace[$legacyAcceptanceIndex].State -ne 'READY') 'New readiness accepted the temporary server.'
}

Invoke-Test 'three-stable-sql-probes-are-required' {
    $result = Invoke-Readiness @($stableA, $stableA, $stableA)
    Assert-Condition ($result.StableProbeCount -eq 3) 'Stable probe count was not three.'
    Assert-Condition ($result.State -eq 'READY') 'Stable final server did not become ready.'
}

Invoke-Test 'postmaster-start-change-resets-stability' {
    $result = Invoke-Readiness @($stableA, $stableA, $stableB, $stableB, $stableB)
    Assert-Condition ($result.PostmasterStartTime -eq $stableB.PostmasterStartTime) 'Changed postmaster was not selected.'
    Assert-Condition ($result.Trace[2].ConsecutiveStableProbes -eq 1) 'Postmaster change did not reset stability.'
}

Invoke-Test 'transient-sql-failure-resets-stability' {
    $result = Invoke-Readiness @($stableA, $stableA, $transientSqlFailure, $stableA, $stableA, $stableA)
    Assert-Condition ($result.Trace[2].ConsecutiveStableProbes -eq 0) 'Transient SQL failure did not reset stability.'
}

Invoke-Test 'final-process-evidence-loss-resets-stability' {
    $result = Invoke-Readiness @($stableA, $stableA, $finalStarting, $stableA, $stableA, $stableA)
    Assert-Condition ($result.Trace[2].ConsecutiveStableProbes -eq 0) 'Final-process evidence loss did not reset stability.'
}

Invoke-Test 'stable-final-server-passes' {
    $result = Invoke-Readiness @($starting, $finalStarting, $stableA, $stableA, $stableA)
    Assert-Condition ($result.ServerVersion -eq '16.10') 'Final server version was not retained.'
}

Invoke-Test 'container-exit-fails' {
    $failed = $false
    try { Invoke-Readiness @((New-Observation -Status 'exited' -Running $false)) | Out-Null }
    catch { $failed = $_.Exception.Message -match "terminal state 'exited'" }
    Assert-Condition $failed 'Container exit did not fail readiness.'
}

Invoke-Test 'repeated-restart-fails' {
    $restartOne = New-Observation -Status 'running' -Running $true -RestartCount 1
    $restartTwo = New-Observation -Status 'running' -Running $true -RestartCount 2
    $restartThree = New-Observation -Status 'running' -Running $true -RestartCount 3
    $failed = $false
    try { Invoke-Readiness @($restartOne, $restartTwo, $restartThree) | Out-Null }
    catch { $failed = $_.Exception.Message -match 'repeatedly restarted' }
    Assert-Condition $failed 'Repeated restart did not fail readiness.'
}

Invoke-Test 'timeout-fails-with-phase' {
    $failed = $false
    try {
        Wait-AnnexE1V17PostgresFinalReadiness -ContainerName 'test-pg' -DatabaseName 'annex_test' `
            -ProbeProvider (New-SequenceProvider @($starting)) -Delay { param($Milliseconds) } `
            -UtcNow (New-AdvancingClock -StepMilliseconds 600) -TimeoutSeconds 1 | Out-Null
    }
    catch { $failed = $_.Exception.Message -match 'TIMED_OUT.*STARTING' }
    Assert-Condition $failed 'Timeout did not identify its readiness phase.'
}

Invoke-Test 'wrong-database-fails' {
    $wrongDatabase = New-Observation -PgIsReady $true -InitializationComplete $true -PidOneCommand 'postgres' -SqlError 'database "wrong" does not exist'
    $failed = $false
    try { Invoke-Readiness @($wrongDatabase) | Out-Null }
    catch { $failed = $_.Exception.Message -match 'intended database target does not exist' }
    Assert-Condition $failed 'Wrong database did not fail readiness.'
}

Invoke-Test 'authentication-failure-fails' {
    $authenticationFailure = New-Observation -PgIsReady $true -InitializationComplete $true -PidOneCommand 'postgres' -SqlError 'password authentication failed for user postgres'
    $failed = $false
    try { Invoke-Readiness @($authenticationFailure) | Out-Null }
    catch { $failed = $_.Exception.Message -match 'authentication or role validation failed' }
    Assert-Condition $failed 'Authentication failure did not fail readiness.'
}

Invoke-Test 'malformed-sql-evidence-fails' {
    $malformed = New-Observation -PgIsReady $true -InitializationComplete $true -PidOneCommand 'postgres' -SqlError 'SQL readiness response was malformed'
    $failed = $false
    try { Invoke-Readiness @($malformed) | Out-Null }
    catch { $failed = $_.Exception.Message -match 'SQL readiness evidence was malformed' }
    Assert-Condition $failed 'Malformed SQL evidence did not fail readiness.'
}

Invoke-Test 'runner-cleanup-remains-in-finally' {
    $runner = Get-Content -LiteralPath (Join-Path $repoRoot 'scripts\Invoke-AnnexE1V17IsolatedExecution.ps1') -Raw
    Assert-Condition ($runner -match 'finally\s*\{[\s\S]*Remove-OwnedResource container[\s\S]*Remove-OwnedResource volume[\s\S]*Remove-OwnedResource network') 'Runner cleanup is not protected by finally.'
}

Invoke-Test 'ordinal-zero-waits-before-intentional-failure' {
    $runner = Get-Content -LiteralPath (Join-Path $repoRoot 'scripts\Invoke-AnnexE1V17IsolatedExecution.ps1') -Raw
    $waitIndex = $runner.IndexOf('Wait-AnnexE1V17PostgresFinalReadiness', [StringComparison]::Ordinal)
    $versionIndex = $runner.IndexOf("Invoke-CheckedNative 'read PostgreSQL version'", [StringComparison]::Ordinal)
    $failureIndex = $runner.IndexOf("if (`$InjectFailure) { throw 'INTENTIONAL_FAILURE_PATH_PROBE' }", [StringComparison]::Ordinal)
    Assert-Condition ($waitIndex -ge 0 -and $waitIndex -lt $versionIndex -and $versionIndex -lt $failureIndex) 'Ordinal-0 ordering does not enforce final readiness before the version query and failure probe.'
}

Write-Output "ANNEX_E1_V17_POSTGRES_READINESS_TESTS=PASS"
Write-Output "TESTS_PASSED=$script:passed"
