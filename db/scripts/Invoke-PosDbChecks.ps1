<#
.SYNOPSIS
Runs POS Server database repository validation, optional rebuild, inventory, and drift checks.

.DESCRIPTION
This script validates the POS Server database SQL state under db/state using the repository
manifest and inventory configuration. Static checks do not require PostgreSQL. Rebuild,
Inventory, and Drift modes require psql and an explicit connection string for a disposable
or otherwise approved non-production database. psql may be local on PATH or executed through
Docker with -UseDockerPsql.

Repository SQL is the source of truth. Drift is reported only and is never promoted back
into repository artifacts by this script.

.PARAMETER Mode
Check mode to run: Static, Rebuild, Inventory, Drift, or All.

.PARAMETER ConnectionString
Explicit PostgreSQL connection string used by Rebuild, Inventory, and Drift modes.
Do not pass production or shared authority database connection strings.

.PARAMETER DatabaseName
Optional database name used for conservative safety checks and evidence context.

.PARAMETER EvidenceDir
Optional directory for JSON and text evidence output. If omitted, no evidence files are written.

.PARAMETER UseDockerPsql
Run psql through a disposable Docker PostgreSQL client container instead of local psql.

.PARAMETER DockerImage
Docker image to use for psql execution when -UseDockerPsql is provided.

.PARAMETER DockerNetwork
Optional Docker network for the psql client container.

.PARAMETER DockerHostAlias
Host alias documented for Docker connection strings. Defaults to host.docker.internal.

.PARAMETER DockerContainerName
Container name for the disposable psql client container.

.EXAMPLE
.\db\scripts\Invoke-PosDbChecks.ps1 -Mode Static -EvidenceDir .\db\validation\evidence\local-static

.EXAMPLE
.\db\scripts\Invoke-PosDbChecks.ps1 -Mode All -ConnectionString $env:POSSERVER_DB_URL -EvidenceDir .\db\validation\evidence\local-all

.EXAMPLE
.\db\scripts\Invoke-PosDbChecks.ps1 -Mode All -UseDockerPsql -ConnectionString $env:POSSERVER_DB_URL -EvidenceDir .\db\validation\evidence\docker-all
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Static', 'Rebuild', 'Inventory', 'Drift', 'All')]
    [string] $Mode = 'Static',

    [Parameter()]
    [string] $ConnectionString,

    [Parameter()]
    [string] $DatabaseName,

    [Parameter()]
    [string] $EvidenceDir,

    [Parameter()]
    [switch] $UseDockerPsql,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $DockerImage = 'postgres:16-alpine',

    [Parameter()]
    [string] $DockerNetwork,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $DockerHostAlias = 'host.docker.internal',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $DockerContainerName = 'posserver-db-checks-psql'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptRoot '..\..')).Path
$ManifestPath = Join-Path $RepoRoot 'db\rebuild\pos_sql_apply_order.txt'
$ExpectedInventoryPath = Join-Path $RepoRoot 'db\validation\pos_expected_inventory.json'
$ProhibitedPatternsPath = Join-Path $RepoRoot 'db\validation\pos_prohibited_patterns.json'
$RunStamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')

function New-CheckResult {
    param([string] $Name)

    return [ordered]@{
        mode = $Name
        started_at_utc = (Get-Date).ToUniversalTime().ToString('o')
        finished_at_utc = $null
        status = 'running'
        errors = @()
        warnings = @()
        details = [ordered]@{}
    }
}

function Resolve-RepoFile {
    param([string] $RelativePath)

    $normalized = $RelativePath -replace '/', [IO.Path]::DirectorySeparatorChar
    return Join-Path $RepoRoot $normalized
}

function ConvertTo-RepoRelativePath {
    param([string] $Path)

    $resolved = (Resolve-Path $Path).Path
    $root = $RepoRoot.TrimEnd('\')
    if (-not $resolved.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path is outside repository root: $Path"
    }

    return $resolved.Substring($root.Length + 1).Replace('\', '/')
}

function Get-ManifestEntries {
    if (-not (Test-Path -LiteralPath $ManifestPath)) {
        throw "Manifest file not found: $ManifestPath"
    }

    return @(Get-Content -LiteralPath $ManifestPath |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ -and -not $_.StartsWith('#') })
}

function Get-StateSqlFiles {
    $stateRoot = Join-Path $RepoRoot 'db\state'
    return @(Get-ChildItem -LiteralPath $stateRoot -Recurse -File -Filter '*.sql' |
        ForEach-Object { ConvertTo-RepoRelativePath $_.FullName } |
        Sort-Object)
}

function Get-JsonFile {
    param([string] $Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required configuration file not found: $Path"
    }

    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Remove-SqlComments {
    param([string] $Sql)

    $withoutBlockComments = [regex]::Replace($Sql, '/\*.*?\*/', '', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    return [regex]::Replace($withoutBlockComments, '(?m)--.*$', '')
}

function Test-SnakeIdentifier {
    param([string] $Identifier)

    return $Identifier -cmatch '^[a-z][a-z0-9_]*$'
}

function Add-ValidationError {
    param(
        [hashtable] $Result,
        [string] $Message
    )

    $Result.errors += $Message
}

function Add-ValidationWarning {
    param(
        [hashtable] $Result,
        [string] $Message
    )

    $Result.warnings += $Message
}

function Complete-Result {
    param(
        [hashtable] $Result,
        [string[]] $SummaryLines
    )

    $Result.finished_at_utc = (Get-Date).ToUniversalTime().ToString('o')
    if ($Result.errors.Count -gt 0) {
        $Result.status = 'failed'
    }
    else {
        $Result.status = 'passed'
    }

    Write-Evidence -ModeName $Result.mode -Result $Result -SummaryLines $SummaryLines

    if ($Result.errors.Count -gt 0) {
        throw "$($Result.mode) checks failed with $($Result.errors.Count) error(s)."
    }

    return $Result
}

function Write-Evidence {
    param(
        [string] $ModeName,
        [hashtable] $Result,
        [string[]] $SummaryLines
    )

    if ([string]::IsNullOrWhiteSpace($EvidenceDir)) {
        return
    }

    $evidencePath = Resolve-RepoFile $EvidenceDir
    New-Item -ItemType Directory -Path $evidencePath -Force | Out-Null

    $safeMode = $ModeName.ToLowerInvariant()
    $jsonPath = Join-Path $evidencePath "pos-db-checks-$safeMode-$RunStamp.local.json"
    $textPath = Join-Path $evidencePath "pos-db-checks-$safeMode-$RunStamp.local.txt"

    $Result | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $jsonPath -Encoding UTF8
    $SummaryLines | Set-Content -LiteralPath $textPath -Encoding UTF8
}

function Assert-PsqlAvailable {
    $command = Get-Command psql -ErrorAction SilentlyContinue
    if (-not $command) {
        throw 'psql was not found on PATH. Install PostgreSQL client tools or run Static mode only.'
    }
}

function Assert-DockerAvailable {
    $command = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $command) {
        throw 'docker was not found on PATH. Install Docker, start Docker Desktop, or run without -UseDockerPsql with local psql.'
    }

    $output = & docker version --format '{{.Client.Version}}' 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Docker is unavailable or not running. Docker output: $($output -join [Environment]::NewLine)"
    }
}

function Assert-PsqlRunnerAvailable {
    if ($UseDockerPsql) {
        Assert-DockerAvailable
    }
    else {
        Assert-PsqlAvailable
    }
}

function Assert-ConnectionStringProvided {
    if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
        throw 'A PostgreSQL connection string is required for this mode.'
    }
}

function Assert-DisposableDatabaseTarget {
    $targetText = @($ConnectionString, $DatabaseName) -join ' '
    if ($targetText -match '(?i)(prod|production|live|shared|authority|central[_-]?pms)') {
        throw 'Refusing to run against a database target that looks production/shared/authority-owned. Use a disposable validation database.'
    }
}

function ConvertTo-ContainerPath {
    param([string] $RelativePath)

    $normalized = $RelativePath.Replace('\', '/').TrimStart('/')
    return "/work/$normalized"
}

function Get-DockerPsqlBaseArgs {
    $volumeSpec = "${RepoRoot}:/work:ro"
    $args = New-Object System.Collections.Generic.List[string]
    $args.Add('run')
    $args.Add('--rm')
    $args.Add('--name')
    $args.Add($DockerContainerName)
    $args.Add('--volume')
    $args.Add($volumeSpec)
    $args.Add('--workdir')
    $args.Add('/work')
    $args.Add('--entrypoint')
    $args.Add('psql')

    if (-not [string]::IsNullOrWhiteSpace($DockerNetwork)) {
        $args.Add('--network')
        $args.Add($DockerNetwork)
    }

    if (-not [string]::IsNullOrWhiteSpace($DockerHostAlias)) {
        $args.Add('--add-host')
        $args.Add("${DockerHostAlias}:host-gateway")
    }

    $args.Add($DockerImage)
    return ,$args
}

function Invoke-DockerPsqlCommand {
    param(
        [string] $Sql,
        [string] $FilePath
    )

    $dockerArgs = Get-DockerPsqlBaseArgs
    $dockerArgs.Add($ConnectionString)
    $dockerArgs.Add('-v')
    $dockerArgs.Add('ON_ERROR_STOP=1')

    if (-not [string]::IsNullOrWhiteSpace($Sql)) {
        $dockerArgs.Add('-t')
        $dockerArgs.Add('-A')
        $dockerArgs.Add('-c')
        $dockerArgs.Add($Sql)
    }
    elseif (-not [string]::IsNullOrWhiteSpace($FilePath)) {
        $repoRelative = ConvertTo-RepoRelativePath $FilePath
        $containerPath = ConvertTo-ContainerPath $repoRelative
        $dockerArgs.Add('-f')
        $dockerArgs.Add($containerPath)
    }
    else {
        throw 'Invoke-DockerPsqlCommand requires either Sql or FilePath.'
    }

    $output = & docker @($dockerArgs.ToArray()) 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Docker psql execution failed. Verify Docker is running, image '$DockerImage' is available, and the connection string targets a disposable database. Output: $($output -join [Environment]::NewLine)"
    }

    return $output
}

function Invoke-PsqlCommand {
    param(
        [string] $Sql,
        [string] $FilePath
    )

    if ($UseDockerPsql) {
        return Invoke-DockerPsqlCommand -Sql $Sql -FilePath $FilePath
    }

    if (-not [string]::IsNullOrWhiteSpace($Sql)) {
        $output = & psql $ConnectionString -v ON_ERROR_STOP=1 -t -A -c $Sql 2>&1
    }
    elseif (-not [string]::IsNullOrWhiteSpace($FilePath)) {
        $output = & psql $ConnectionString -v ON_ERROR_STOP=1 -f $FilePath 2>&1
    }
    else {
        throw 'Invoke-PsqlCommand requires either Sql or FilePath.'
    }

    if ($LASTEXITCODE -ne 0) {
        throw ($output -join [Environment]::NewLine)
    }

    return $output
}

function Get-DatabaseInventory {
    Assert-ConnectionStringProvided
    Assert-PsqlRunnerAvailable

    $query = @"
select jsonb_pretty(jsonb_build_object(
  'schemas', (
    select coalesce(jsonb_agg(nspname order by nspname), '[]'::jsonb)
    from pg_namespace
    where nspname = 'pos'
  ),
  'tables', (
    select coalesce(jsonb_agg(table_schema || '.' || table_name order by table_schema, table_name), '[]'::jsonb)
    from information_schema.tables
    where table_schema = 'pos'
      and table_type = 'BASE TABLE'
  ),
  'constraints', (
    select coalesce(jsonb_agg(jsonb_build_object(
      'schema', table_schema,
      'table', table_name,
      'constraint', constraint_name,
      'type', constraint_type
    ) order by table_schema, table_name, constraint_name), '[]'::jsonb)
    from information_schema.table_constraints
    where table_schema = 'pos'
  ),
  'indexes', (
    select coalesce(jsonb_agg(jsonb_build_object(
      'schema', schemaname,
      'table', tablename,
      'index', indexname
    ) order by schemaname, tablename, indexname), '[]'::jsonb)
    from pg_indexes
    where schemaname = 'pos'
  ),
  'functions', (
    select coalesce(jsonb_agg(n.nspname || '.' || p.proname order by n.nspname, p.proname), '[]'::jsonb)
    from pg_proc p
    join pg_namespace n on n.oid = p.pronamespace
    where n.nspname = 'pos'
  ),
  'triggers', (
    select coalesce(jsonb_agg(event_object_schema || '.' || event_object_table || '.' || trigger_name order by event_object_schema, event_object_table, trigger_name), '[]'::jsonb)
    from information_schema.triggers
    where event_object_schema = 'pos'
  ),
  'extensions', (
    select coalesce(jsonb_agg(extname order by extname), '[]'::jsonb)
    from pg_extension
  ),
  'sequences', (
    select coalesce(jsonb_agg(sequence_schema || '.' || sequence_name order by sequence_schema, sequence_name), '[]'::jsonb)
    from information_schema.sequences
    where sequence_schema = 'pos'
  )
));
"@

    $json = (Invoke-PsqlCommand -Sql $query) -join [Environment]::NewLine
    return $json | ConvertFrom-Json
}

function Compare-Inventory {
    param(
        [hashtable] $Result,
        [object] $Inventory,
        [string] $ModeName
    )

    $expected = Get-JsonFile -Path $ExpectedInventoryPath
    $expectedSchemas = @($expected.expected_schemas | Sort-Object)
    $actualSchemas = @($Inventory.schemas | Sort-Object)
    $expectedTables = @($expected.expected_tables | Sort-Object)
    $actualTables = @($Inventory.tables | Sort-Object)

    $missingSchemas = @(Compare-Object -ReferenceObject $expectedSchemas -DifferenceObject $actualSchemas |
        Where-Object { $_.SideIndicator -eq '<=' } |
        ForEach-Object { $_.InputObject })
    $unexpectedSchemas = @(Compare-Object -ReferenceObject $expectedSchemas -DifferenceObject $actualSchemas |
        Where-Object { $_.SideIndicator -eq '=>' } |
        ForEach-Object { $_.InputObject })
    $missingTables = @(Compare-Object -ReferenceObject $expectedTables -DifferenceObject $actualTables |
        Where-Object { $_.SideIndicator -eq '<=' } |
        ForEach-Object { $_.InputObject })
    $unexpectedTables = @(Compare-Object -ReferenceObject $expectedTables -DifferenceObject $actualTables |
        Where-Object { $_.SideIndicator -eq '=>' } |
        ForEach-Object { $_.InputObject })

    $nonDefaultExtensions = @($Inventory.extensions | Where-Object { $_ -ne 'plpgsql' } | Sort-Object)

    $Result.details.inventory = [ordered]@{
        expected_schema_count = $expectedSchemas.Count
        actual_schema_count = $actualSchemas.Count
        expected_table_count = $expectedTables.Count
        actual_table_count = $actualTables.Count
        missing_schemas = $missingSchemas
        unexpected_schemas = $unexpectedSchemas
        missing_tables = $missingTables
        unexpected_tables = $unexpectedTables
        constraints_count = @($Inventory.constraints).Count
        indexes_count = @($Inventory.indexes).Count
        functions = @($Inventory.functions)
        triggers = @($Inventory.triggers)
        extensions = @($Inventory.extensions)
        non_default_extensions = $nonDefaultExtensions
        sequences = @($Inventory.sequences)
        drift_rule = 'Repository expected inventory is source of truth. Drift is reported only and is not promoted.'
    }

    foreach ($schema in $missingSchemas) { Add-ValidationError $Result "$ModeName drift: missing expected schema $schema." }
    foreach ($schema in $unexpectedSchemas) { Add-ValidationError $Result "$ModeName drift: unexpected schema $schema." }
    foreach ($table in $missingTables) { Add-ValidationError $Result "$ModeName drift: missing expected table $table." }
    foreach ($table in $unexpectedTables) { Add-ValidationError $Result "$ModeName drift: unexpected table $table." }
    foreach ($function in @($Inventory.functions)) { Add-ValidationError $Result "$ModeName prohibited object: function $function exists in pos schema." }
    foreach ($trigger in @($Inventory.triggers)) { Add-ValidationError $Result "$ModeName prohibited object: trigger $trigger exists in pos schema." }
    foreach ($sequence in @($Inventory.sequences)) { Add-ValidationError $Result "$ModeName prohibited object: sequence $sequence exists in pos schema." }
    foreach ($extension in $nonDefaultExtensions) { Add-ValidationError $Result "$ModeName prohibited object: non-default extension $extension exists in database." }
}

function Invoke-StaticChecks {
    $result = New-CheckResult -Name 'Static'
    $summary = New-Object System.Collections.Generic.List[string]
    $summary.Add('POS Server DB static validation')
    $summary.Add("Repository: $RepoRoot")
    $summary.Add('Repository SQL state is source of truth. Local drift promotion is prohibited.')

    if (-not (Test-Path -LiteralPath $ManifestPath)) {
        Add-ValidationError $result "Manifest file does not exist: $ManifestPath"
    }
    if (-not (Test-Path -LiteralPath $ExpectedInventoryPath)) {
        Add-ValidationError $result "Expected inventory file does not exist: $ExpectedInventoryPath"
    }
    if (-not (Test-Path -LiteralPath $ProhibitedPatternsPath)) {
        Add-ValidationError $result "Prohibited patterns file does not exist: $ProhibitedPatternsPath"
    }

    if ($result.errors.Count -gt 0) {
        return Complete-Result -Result $result -SummaryLines $summary.ToArray()
    }

    $manifestEntries = Get-ManifestEntries
    $stateSqlFiles = Get-StateSqlFiles
    $expectedInventory = Get-JsonFile -Path $ExpectedInventoryPath
    $prohibitedConfig = Get-JsonFile -Path $ProhibitedPatternsPath

    $result.details.manifest = [ordered]@{
        path = 'db/rebuild/pos_sql_apply_order.txt'
        entry_count = $manifestEntries.Count
        state_sql_count = $stateSqlFiles.Count
    }
    $result.details.source_of_truth = $expectedInventory.source_of_truth

    foreach ($entry in $manifestEntries) {
        if (-not (Test-Path -LiteralPath (Resolve-RepoFile $entry))) {
            Add-ValidationError $result "Manifest entry does not exist: $entry"
        }
    }

    $missingFromManifest = @(Compare-Object -ReferenceObject ($stateSqlFiles | Sort-Object) -DifferenceObject ($manifestEntries | Sort-Object) |
        Where-Object { $_.SideIndicator -eq '<=' } |
        ForEach-Object { $_.InputObject })
    $extraInManifest = @(Compare-Object -ReferenceObject ($stateSqlFiles | Sort-Object) -DifferenceObject ($manifestEntries | Sort-Object) |
        Where-Object { $_.SideIndicator -eq '=>' } |
        ForEach-Object { $_.InputObject })

    foreach ($file in $missingFromManifest) { Add-ValidationError $result "db/state SQL file is missing from manifest: $file" }
    foreach ($file in $extraInManifest) { Add-ValidationError $result "Manifest lists a file that is not a db/state SQL file: $file" }

    if ($expectedInventory.expected_table_count -ne @($expectedInventory.expected_tables).Count) {
        Add-ValidationError $result "Expected inventory table count $($expectedInventory.expected_table_count) does not match listed tables $(@($expectedInventory.expected_tables).Count)."
    }

    $regexOptions = [System.Text.RegularExpressions.RegexOptions]::IgnoreCase
    $identifierFindings = New-Object System.Collections.Generic.List[string]
    $namingFindings = New-Object System.Collections.Generic.List[string]
    $prohibitedFindings = New-Object System.Collections.Generic.List[string]

    foreach ($relativeFile in $stateSqlFiles) {
        $path = Resolve-RepoFile $relativeFile
        $rawSql = Get-Content -LiteralPath $path -Raw
        $sql = Remove-SqlComments -Sql $rawSql

        foreach ($pattern in $prohibitedConfig.patterns) {
            if ([regex]::IsMatch($rawSql, $pattern.regex, $regexOptions)) {
                $message = "$($pattern.id) $($pattern.description) Matched in $relativeFile"
                $prohibitedFindings.Add($message)
                Add-ValidationError $result $message
            }
        }

        if ([regex]::IsMatch($sql, '"[A-Za-z_][A-Za-z0-9_]*"')) {
            $message = "Quoted identifier detected in $relativeFile."
            $namingFindings.Add($message)
            Add-ValidationError $result $message
        }

        $tableMatches = [regex]::Matches($sql, '\bCREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?([A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_]*)', $regexOptions)
        foreach ($match in $tableMatches) {
            $objectName = $match.Groups[1].Value
            if (-not $objectName.StartsWith('pos.', [StringComparison]::Ordinal)) {
                $message = "CREATE TABLE is not schema-qualified with pos schema in ${relativeFile}: $objectName"
                $namingFindings.Add($message)
                Add-ValidationError $result $message
            }

            foreach ($part in $objectName.Split('.')) {
                if (-not (Test-SnakeIdentifier -Identifier $part)) {
                    $message = "Object identifier is not lowercase snake_case in ${relativeFile}: $part"
                    $namingFindings.Add($message)
                    Add-ValidationError $result $message
                }
            }
        }

        $unqualifiedTableMatches = [regex]::Matches($sql, '\bCREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?([A-Za-z_][A-Za-z0-9_]*)(?=\s*\()', $regexOptions)
        foreach ($match in $unqualifiedTableMatches) {
            $message = "Unqualified CREATE TABLE detected in ${relativeFile}: $($match.Groups[1].Value)"
            $namingFindings.Add($message)
            Add-ValidationError $result $message
        }

        $constraintMatches = [regex]::Matches($sql, '\bCONSTRAINT\s+([A-Za-z_][A-Za-z0-9_]*)', $regexOptions)
        foreach ($match in $constraintMatches) {
            $identifier = $match.Groups[1].Value
            $byteCount = [Text.Encoding]::UTF8.GetByteCount($identifier)
            if ($byteCount -gt 63) {
                $message = "Identifier exceeds PostgreSQL 63-byte limit in ${relativeFile}: $identifier ($byteCount bytes)"
                $identifierFindings.Add($message)
                Add-ValidationError $result $message
            }
            if (-not (Test-SnakeIdentifier -Identifier $identifier)) {
                $message = "Constraint identifier is not lowercase snake_case in ${relativeFile}: $identifier"
                $namingFindings.Add($message)
                Add-ValidationError $result $message
            }
        }

        $schemaMatches = [regex]::Matches($sql, '\bCREATE\s+SCHEMA\s+(?:IF\s+NOT\s+EXISTS\s+)?([A-Za-z_][A-Za-z0-9_]*)', $regexOptions)
        foreach ($match in $schemaMatches) {
            $identifier = $match.Groups[1].Value
            if (-not (Test-SnakeIdentifier -Identifier $identifier)) {
                $message = "Schema identifier is not lowercase snake_case in ${relativeFile}: $identifier"
                $namingFindings.Add($message)
                Add-ValidationError $result $message
            }
        }
    }

    $dbFileRoot = Join-Path $RepoRoot 'db'
    $artifactFiles = @(Get-ChildItem -LiteralPath $dbFileRoot -Recurse -File |
        ForEach-Object { ConvertTo-RepoRelativePath $_.FullName })
    $rootAtlas = Join-Path $RepoRoot 'atlas.hcl'
    if (Test-Path -LiteralPath $rootAtlas) {
        $artifactFiles += 'atlas.hcl'
    }

    foreach ($artifactPattern in $prohibitedConfig.artifact_patterns) {
        foreach ($file in $artifactFiles) {
            if ([regex]::IsMatch($file, $artifactPattern.regex, $regexOptions)) {
                Add-ValidationError $result "$($artifactPattern.id) $($artifactPattern.description) Matched artifact path: $file"
            }
        }
    }

    $result.details.static_checks = [ordered]@{
        manifest_entries = $manifestEntries
        state_sql_files = $stateSqlFiles
        missing_from_manifest = $missingFromManifest
        extra_in_manifest = $extraInManifest
        prohibited_findings = $prohibitedFindings.ToArray()
        identifier_findings = $identifierFindings.ToArray()
        naming_findings = $namingFindings.ToArray()
        no_local_drift_promotion_rule = 'Static checks do not inspect or promote local database state.'
    }

    $summary.Add("Manifest entries: $($manifestEntries.Count)")
    $summary.Add("db/state SQL files: $($stateSqlFiles.Count)")
    $summary.Add("Missing manifest entries: $($missingFromManifest.Count)")
    $summary.Add("Extra manifest entries: $($extraInManifest.Count)")
    $summary.Add("Prohibited pattern findings: $($prohibitedFindings.Count)")
    $summary.Add("Identifier length findings: $($identifierFindings.Count)")
    $summary.Add("Naming findings: $($namingFindings.Count)")

    return Complete-Result -Result $result -SummaryLines $summary.ToArray()
}

function Invoke-RebuildChecks {
    Assert-ConnectionStringProvided
    Assert-PsqlRunnerAvailable
    Assert-DisposableDatabaseTarget

    $result = New-CheckResult -Name 'Rebuild'
    $summary = New-Object System.Collections.Generic.List[string]
    $summary.Add('POS Server DB rebuild check')
    $summary.Add('Target connection string provided: yes')
    if (-not [string]::IsNullOrWhiteSpace($DatabaseName)) {
        $summary.Add("Database name: $DatabaseName")
    }

    $appliedFiles = New-Object System.Collections.Generic.List[string]
    foreach ($entry in Get-ManifestEntries) {
        $file = Resolve-RepoFile $entry
        Invoke-PsqlCommand -FilePath $file | Out-Null
        $appliedFiles.Add($entry)
    }

    $result.details.applied_files = $appliedFiles.ToArray()
    $result.details.applied_file_count = $appliedFiles.Count
    $result.details.production_safety = 'Script does not create or drop databases and refuses obvious production/shared target names.'
    $result.details.psql_runner = if ($UseDockerPsql) { 'docker' } else { 'local' }
    if ($UseDockerPsql) {
        $result.details.docker = [ordered]@{
            image = $DockerImage
            network = $DockerNetwork
            host_alias = $DockerHostAlias
            container_name = $DockerContainerName
            workdir = '/work'
        }
    }
    $summary.Add("Applied files: $($appliedFiles.Count)")

    return Complete-Result -Result $result -SummaryLines $summary.ToArray()
}

function Invoke-InventoryChecks {
    param([string] $ResultMode = 'Inventory')

    Assert-ConnectionStringProvided
    Assert-DisposableDatabaseTarget

    $result = New-CheckResult -Name $ResultMode
    $summary = New-Object System.Collections.Generic.List[string]
    $summary.Add("POS Server DB $ResultMode check")
    $summary.Add('Repository expected inventory is source of truth.')

    $inventory = Get-DatabaseInventory
    Compare-Inventory -Result $result -Inventory $inventory -ModeName $ResultMode
    $result.details.psql_runner = if ($UseDockerPsql) { 'docker' } else { 'local' }
    if ($UseDockerPsql) {
        $result.details.docker = [ordered]@{
            image = $DockerImage
            network = $DockerNetwork
            host_alias = $DockerHostAlias
            container_name = $DockerContainerName
            workdir = '/work'
        }
    }

    $summary.Add("Expected tables: $($result.details.inventory.expected_table_count)")
    $summary.Add("Actual tables: $($result.details.inventory.actual_table_count)")
    $summary.Add("Missing tables: $(@($result.details.inventory.missing_tables).Count)")
    $summary.Add("Unexpected tables: $(@($result.details.inventory.unexpected_tables).Count)")
    $summary.Add("Functions in pos schema: $(@($result.details.inventory.functions).Count)")
    $summary.Add("Triggers in pos schema: $(@($result.details.inventory.triggers).Count)")
    $summary.Add("Sequences in pos schema: $(@($result.details.inventory.sequences).Count)")
    $summary.Add("Non-default extensions: $(@($result.details.inventory.non_default_extensions).Count)")

    return Complete-Result -Result $result -SummaryLines $summary.ToArray()
}

function Invoke-AllChecks {
    $result = New-CheckResult -Name 'All'
    $summary = New-Object System.Collections.Generic.List[string]
    $summary.Add('POS Server DB all checks')

    Invoke-StaticChecks | Out-Null
    $summary.Add('Static checks completed.')

    if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
        $message = 'ConnectionString was not provided. Rebuild, Inventory, and Drift checks were skipped.'
        Add-ValidationWarning $result $message
        $summary.Add($message)
        $result.details.db_dependent_checks = 'skipped'
    }
    else {
        Invoke-RebuildChecks | Out-Null
        Invoke-InventoryChecks -ResultMode 'Inventory' | Out-Null
        Invoke-InventoryChecks -ResultMode 'Drift' | Out-Null
        $result.details.db_dependent_checks = 'completed'
        $summary.Add('Rebuild, Inventory, and Drift checks completed.')
    }

    return Complete-Result -Result $result -SummaryLines $summary.ToArray()
}

switch ($Mode) {
    'Static' { Invoke-StaticChecks | Out-Null }
    'Rebuild' { Invoke-RebuildChecks | Out-Null }
    'Inventory' { Invoke-InventoryChecks -ResultMode 'Inventory' | Out-Null }
    'Drift' { Invoke-InventoryChecks -ResultMode 'Drift' | Out-Null }
    'All' { Invoke-AllChecks | Out-Null }
}

Write-Host "POS DB checks completed for mode $Mode."
