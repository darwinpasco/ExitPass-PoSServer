Set-StrictMode -Version Latest

function Invoke-AnnexE1V17DockerCapture {
    param([Parameter(Mandatory = $true)][string[]] $Arguments)

    $savedErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & docker @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $savedErrorActionPreference
    }

    return [pscustomobject]@{
        ExitCode = $exitCode
        Text = ($output -join [Environment]::NewLine)
    }
}

function Get-AnnexE1V17PostgresReadinessObservation {
    param(
        [Parameter(Mandatory = $true)][string] $ContainerName,
        [Parameter(Mandatory = $true)][string] $DatabaseName
    )

    $stateResult = Invoke-AnnexE1V17DockerCapture @('inspect', '--format', '{{json .State}}', $ContainerName)
    if ($stateResult.ExitCode -ne 0) {
        return [pscustomobject]@{
            DockerError = 'container inspection failed'
        }
    }

    try {
        $containerState = $stateResult.Text | ConvertFrom-Json
    }
    catch {
        return [pscustomobject]@{
            DockerError = 'container state was not valid JSON'
        }
    }

    $restartResult = Invoke-AnnexE1V17DockerCapture @('inspect', '--format', '{{.RestartCount}}', $ContainerName)
    $restartCount = 0
    if ($restartResult.ExitCode -ne 0 -or -not [int]::TryParse($restartResult.Text.Trim(), [ref] $restartCount)) {
        return [pscustomobject]@{
            DockerError = 'container restart count could not be read'
        }
    }

    $observation = [ordered]@{
        DockerError = $null
        Exists = $true
        Status = [string] $containerState.Status
        Running = [bool] $containerState.Running
        Restarting = [bool] $containerState.Restarting
        RestartCount = $restartCount
        PgIsReady = $false
        InitializationComplete = $false
        PidOneCommand = $null
        SqlSucceeded = $false
        SqlError = $null
        ServerVersion = $null
        PostmasterStartTime = $null
    }

    if (-not $observation.Running) {
        return [pscustomobject] $observation
    }

    $pgIsReady = Invoke-AnnexE1V17DockerCapture @(
        'exec', $ContainerName, 'pg_isready', '--username', 'postgres', '--dbname', $DatabaseName
    )
    $observation.PgIsReady = $pgIsReady.ExitCode -eq 0

    $logs = Invoke-AnnexE1V17DockerCapture @('logs', $ContainerName)
    if ($logs.ExitCode -ne 0) {
        $observation.DockerError = 'container initialization evidence could not be read'
        return [pscustomobject] $observation
    }
    $observation.InitializationComplete = $logs.Text.IndexOf(
        'PostgreSQL init process complete; ready for start up.',
        [StringComparison]::Ordinal
    ) -ge 0

    $pidOne = Invoke-AnnexE1V17DockerCapture @('exec', $ContainerName, 'cat', '/proc/1/comm')
    if ($pidOne.ExitCode -eq 0) {
        $observation.PidOneCommand = $pidOne.Text.Trim()
    }
    elseif ($observation.InitializationComplete) {
        $observation.DockerError = 'PID 1 final-process evidence could not be read'
        return [pscustomobject] $observation
    }

    if ($observation.InitializationComplete -and $observation.PidOneCommand -eq 'postgres') {
        $sql = Invoke-AnnexE1V17DockerCapture @(
            'exec', $ContainerName,
            'psql', '--username', 'postgres', '--dbname', $DatabaseName,
            '--tuples-only', '--no-align', '--field-separator', '|',
            '--command', "SELECT current_setting('server_version'), pg_postmaster_start_time();"
        )
        if ($sql.ExitCode -eq 0) {
            $parts = $sql.Text.Trim().Split('|')
            if ($parts.Count -eq 2 -and $parts[0].Length -gt 0 -and $parts[1].Length -gt 0) {
                $observation.SqlSucceeded = $true
                $observation.ServerVersion = $parts[0]
                $observation.PostmasterStartTime = $parts[1]
            }
            else {
                $observation.SqlError = 'SQL readiness response was malformed'
            }
        }
        else {
            $observation.SqlError = $sql.Text
        }
    }

    return [pscustomobject] $observation
}

function Wait-AnnexE1V17PostgresFinalReadiness {
    param(
        [Parameter(Mandatory = $true)][string] $ContainerName,
        [Parameter(Mandatory = $true)][string] $DatabaseName,
        [scriptblock] $ProbeProvider = $null,
        [scriptblock] $Delay = $null,
        [scriptblock] $UtcNow = $null,
        [ValidateRange(1, 3600)][int] $TimeoutSeconds = 60,
        [ValidateRange(1, 60000)][int] $PollIntervalMilliseconds = 500,
        [ValidateRange(2, 20)][int] $RequiredStableProbes = 3,
        [ValidateRange(2, 20)][int] $RestartFailureThreshold = 3
    )

    if ($null -eq $ProbeProvider) {
        $ProbeProvider = {
            param($Container, $Database)
            Get-AnnexE1V17PostgresReadinessObservation -ContainerName $Container -DatabaseName $Database
        }
    }
    if ($null -eq $Delay) {
        $Delay = { param($Milliseconds) Start-Sleep -Milliseconds $Milliseconds }
    }
    if ($null -eq $UtcNow) {
        $UtcNow = { [DateTime]::UtcNow }
    }

    $trace = New-Object System.Collections.Generic.List[object]
    $startedUtc = [DateTime] (& $UtcNow)
    $deadlineUtc = $startedUtc.AddSeconds($TimeoutSeconds)
    $state = 'STARTING'
    $stablePostmasterStartTime = $null
    $consecutiveSuccesses = 0
    $consecutiveRestartObservations = 0
    $observedRestartEvents = 0
    $lastRestartCount = 0

    while ([DateTime] (& $UtcNow) -lt $deadlineUtc) {
        $capturedUtc = [DateTime] (& $UtcNow)
        $observation = & $ProbeProvider $ContainerName $DatabaseName
        if ($null -eq $observation) {
            throw "PostgreSQL final-readiness FAILED during ${state}: mandatory readiness evidence was unavailable."
        }
        if ($observation.PSObject.Properties.Name -contains 'DockerError' -and $null -ne $observation.DockerError) {
            throw "PostgreSQL final-readiness FAILED during ${state}: $($observation.DockerError)."
        }

        $status = [string] $observation.Status
        $running = [bool] $observation.Running
        $restarting = [bool] $observation.Restarting
        $restartCount = [int] $observation.RestartCount

        if ($restartCount -lt $lastRestartCount) {
            throw "PostgreSQL final-readiness FAILED during ${state}: container restart count moved backwards."
        }
        if ($restartCount -gt $lastRestartCount) {
            $observedRestartEvents += $restartCount - $lastRestartCount
            $lastRestartCount = $restartCount
            $consecutiveSuccesses = 0
            $stablePostmasterStartTime = $null
            if ($observedRestartEvents -ge $RestartFailureThreshold) {
                throw 'PostgreSQL final-readiness FAILED during STARTING: container repeatedly restarted.'
            }
        }

        if ($status -in @('dead', 'exited', 'removing')) {
            throw "PostgreSQL final-readiness FAILED during ${state}: container entered terminal state '$status'."
        }

        if ($restarting) {
            $state = 'STARTING'
            $consecutiveSuccesses = 0
            $stablePostmasterStartTime = $null
            $consecutiveRestartObservations++
            $trace.Add([pscustomobject]@{
                CapturedUtc = $capturedUtc.ToString('o'); State = $state; Status = $status
                Running = $running; Restarting = $restarting; RestartCount = $restartCount
                PgIsReady = $false; InitializationComplete = $false; PidOneCommand = $null
                SqlSucceeded = $false; ServerVersion = $null; PostmasterStartTime = $null
                ConsecutiveStableProbes = 0
            })
            if ($consecutiveRestartObservations -ge $RestartFailureThreshold) {
                throw "PostgreSQL final-readiness FAILED during STARTING: container repeatedly restarted."
            }
            & $Delay $PollIntervalMilliseconds
            continue
        }

        $consecutiveRestartObservations = 0
        if (-not $running) {
            $state = 'STARTING'
            $consecutiveSuccesses = 0
            $stablePostmasterStartTime = $null
        }
        elseif (-not [bool] $observation.InitializationComplete) {
            $state = if ([bool] $observation.PgIsReady) { 'TEMPORARY_SERVER_AVAILABLE' } else { 'INITIALIZING' }
            $consecutiveSuccesses = 0
            $stablePostmasterStartTime = $null
        }
        elseif ([string] $observation.PidOneCommand -ne 'postgres') {
            $state = 'FINAL_SERVER_STARTING'
            $consecutiveSuccesses = 0
            $stablePostmasterStartTime = $null
        }
        elseif (-not [bool] $observation.SqlSucceeded) {
            $state = 'FINAL_SERVER_STABILIZING'
            $consecutiveSuccesses = 0
            $stablePostmasterStartTime = $null
            $sqlError = [string] $observation.SqlError
            if ($sqlError -match 'password authentication failed|no password supplied|role .* does not exist') {
                throw 'PostgreSQL final-readiness FAILED during FINAL_SERVER_STABILIZING: authentication or role validation failed.'
            }
            if ($sqlError -match 'database .* does not exist') {
                throw 'PostgreSQL final-readiness FAILED during FINAL_SERVER_STABILIZING: intended database target does not exist.'
            }
            if ($sqlError -eq 'SQL readiness response was malformed') {
                throw 'PostgreSQL final-readiness FAILED during FINAL_SERVER_STABILIZING: SQL readiness evidence was malformed.'
            }
        }
        else {
            $state = 'FINAL_SERVER_STABILIZING'
            $postmasterStartTime = [string] $observation.PostmasterStartTime
            $serverVersion = [string] $observation.ServerVersion
            if ([string]::IsNullOrWhiteSpace($postmasterStartTime) -or [string]::IsNullOrWhiteSpace($serverVersion)) {
                throw 'PostgreSQL final-readiness FAILED during FINAL_SERVER_STABILIZING: SQL readiness evidence was incomplete.'
            }
            if ($stablePostmasterStartTime -ceq $postmasterStartTime) {
                $consecutiveSuccesses++
            }
            else {
                $stablePostmasterStartTime = $postmasterStartTime
                $consecutiveSuccesses = 1
            }
        }

        $trace.Add([pscustomobject]@{
            CapturedUtc = $capturedUtc.ToString('o'); State = $state; Status = $status
            Running = $running; Restarting = $restarting; RestartCount = $restartCount
            PgIsReady = [bool] $observation.PgIsReady
            InitializationComplete = [bool] $observation.InitializationComplete
            PidOneCommand = [string] $observation.PidOneCommand
            SqlSucceeded = [bool] $observation.SqlSucceeded
            ServerVersion = [string] $observation.ServerVersion
            PostmasterStartTime = [string] $observation.PostmasterStartTime
            ConsecutiveStableProbes = $consecutiveSuccesses
        })

        if ($consecutiveSuccesses -ge $RequiredStableProbes) {
            $state = 'READY'
            return [pscustomobject]@{
                State = $state
                ServerVersion = [string] $observation.ServerVersion
                PostmasterStartTime = $stablePostmasterStartTime
                StableProbeCount = $consecutiveSuccesses
                StartedUtc = $startedUtc.ToString('o')
                ReadyUtc = $capturedUtc.ToString('o')
                Trace = $trace.ToArray()
            }
        }

        & $Delay $PollIntervalMilliseconds
    }

    throw "PostgreSQL final-readiness TIMED_OUT after $TimeoutSeconds seconds during $state."
}
