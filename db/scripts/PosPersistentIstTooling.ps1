Set-StrictMode -Version Latest

function Assert-PosPersistentIstTarget {
    param(
        [Parameter(Mandatory)][string] $ContainerName,
        [Parameter(Mandatory)][string] $DatabaseName,
        [Parameter(Mandatory)][string] $DatabaseUser
    )

    if ($DatabaseName -notmatch '(?i)(ist|validation|local|test|disposable)' -or
        $DatabaseName -match '(?i)(prod|production|live|shared|authority|central_pms)') {
        throw "Refusing non-IST or production-like database name: $DatabaseName"
    }
    foreach ($value in @($ContainerName, $DatabaseName, $DatabaseUser)) {
        if ($value -notmatch '^[A-Za-z0-9_.-]+$') {
            throw 'Container, database, and user identifiers may contain only letters, numbers, dot, underscore, and hyphen.'
        }
    }
}

function Invoke-PosPersistentIstPsql {
    param(
        [Parameter(Mandatory)][string] $Sql,
        [Parameter(Mandatory)][string] $ContainerName,
        [Parameter(Mandatory)][string] $DatabaseName,
        [Parameter(Mandatory)][string] $DatabaseUser,
        [switch] $Scalar
    )

    $arguments = "exec -i $ContainerName psql -X -v ON_ERROR_STOP=1 -U $DatabaseUser -d $DatabaseName"
    if ($Scalar) { $arguments += ' -qAt' } else { $arguments += ' -q' }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = [Diagnostics.ProcessStartInfo]@{
        FileName = 'docker.exe'
        Arguments = $arguments
        UseShellExecute = $false
        RedirectStandardInput = $true
        RedirectStandardOutput = $true
        RedirectStandardError = $true
    }
    try {
        [void]$process.Start()
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.StandardInput.Write($Sql)
        $process.StandardInput.Close()
        $process.WaitForExit()
        $stdout = $stdoutTask.GetAwaiter().GetResult().Trim()
        $stderr = $stderrTask.GetAwaiter().GetResult().Trim()
        if ($process.ExitCode -ne 0) { throw "POS persistent IST SQL failed: $stderr" }
        return $stdout
    }
    finally {
        $process.Dispose()
    }
}

function New-PosPersistentIstUuidV5 {
    param(
        [Parameter(Mandatory)][Guid] $Namespace,
        [Parameter(Mandatory)][string] $Name
    )

    $namespaceBytes = $Namespace.ToByteArray()
    [array]::Reverse($namespaceBytes, 0, 4)
    [array]::Reverse($namespaceBytes, 4, 2)
    [array]::Reverse($namespaceBytes, 6, 2)
    $nameBytes = [Text.Encoding]::UTF8.GetBytes($Name)
    $input = [byte[]]::new($namespaceBytes.Length + $nameBytes.Length)
    [array]::Copy($namespaceBytes, 0, $input, 0, $namespaceBytes.Length)
    [array]::Copy($nameBytes, 0, $input, $namespaceBytes.Length, $nameBytes.Length)
    $sha1 = [Security.Cryptography.SHA1]::Create()
    try { $hash = $sha1.ComputeHash($input) } finally { $sha1.Dispose() }
    [byte[]] $uuidBytes = $hash[0..15]
    $uuidBytes[6] = ($uuidBytes[6] -band 0x0f) -bor 0x50
    $uuidBytes[8] = ($uuidBytes[8] -band 0x3f) -bor 0x80
    [array]::Reverse($uuidBytes, 0, 4)
    [array]::Reverse($uuidBytes, 4, 2)
    [array]::Reverse($uuidBytes, 6, 2)
    return [Guid]::new($uuidBytes)
}

function Write-PosPersistentIstEvidence {
    param(
        [Parameter(Mandatory)][object] $Value,
        [string] $EvidenceDir,
        [Parameter(Mandatory)][string] $FileName
    )

    if ([string]::IsNullOrWhiteSpace($EvidenceDir)) { return }
    [void](New-Item -ItemType Directory -Path $EvidenceDir -Force)
    $path = Join-Path $EvidenceDir $FileName
    [IO.File]::WriteAllText(
        $path,
        ($Value | ConvertTo-Json -Depth 10),
        [Text.UTF8Encoding]::new($false))
}
