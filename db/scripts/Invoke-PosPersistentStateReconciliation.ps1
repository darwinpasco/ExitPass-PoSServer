<#
.SYNOPSIS
Inspects or additively reconciles an existing non-production POS IST database to selected canonical db/state definitions.

.DESCRIPTION
The repository files under db/state remain authoritative. This helper extracts approved column,
constraint, function, function-comment, and trigger definitions from those files at execution
time. It is an INSPECT-then-APPLY tool:
Apply refuses unprovable historical completion ancestry before mutation, adds columns nullable,
backfills only from immutable payment-finality ancestry, validates every row, and installs the
canonical constraints in one transaction. Historical EJ printable text is never reconstructed.
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Inspect', 'Apply')]
    [string] $Mode = 'Inspect',
    [string] $ContainerName = 'exitpass-pos-ist-persistent-db',
    [string] $DatabaseName = 'exitpass_pos_ist',
    [string] $DatabaseUser = 'exitpass_ist',
    [string] $EvidenceDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$fiscalDocumentStatePath = Join-Path $repoRoot 'db\state\tables\pos.fiscal_documents.sql'
$electronicJournalStatePath = Join-Path $repoRoot 'db\state\tables\pos.electronic_journal_records.sql'
$reportingPeriodAssignmentStatePath = Join-Path $repoRoot 'db\state\tables\pos.fiscal_document_reporting_period_assignment.sql'
$expectedInventoryPath = Join-Path $repoRoot 'db\validation\pos_expected_inventory.json'
$fiscalDocumentState = [IO.File]::ReadAllText($fiscalDocumentStatePath)
$electronicJournalState = [IO.File]::ReadAllText($electronicJournalStatePath)
$reportingPeriodAssignmentState = [IO.File]::ReadAllText($reportingPeriodAssignmentStatePath)
$expectedInventory = [IO.File]::ReadAllText($expectedInventoryPath) | ConvertFrom-Json
$completionConstraints = @('ck_fiscal_documents__completion_basis', 'ck_fiscal_documents__completion_ancestry')
$printableColumn = 'printable_sales_invoice_text'
$printableConstraint = 'ck_ej_records__printable_sales_invoice'
$canonicalRequiredConstraint = 'ck_ej_records__canonical_required'
$legacyEjSemanticHashVersion = 'pos-server-electronic-journal-event-semantic:sha256:v1'
$currentEjSemanticHashVersion = 'pos-server-electronic-journal-event-semantic:sha256:v2'
$reportingFunction = 'pos.validate_fiscal_document_reporting_period_assignment'
$reportingTrigger = 'trg_fiscal_documents_reporting_period_assignment'

if ($DatabaseName -notmatch '(?i)(ist|validation|local|test|disposable)' -or
    $DatabaseName -match '(?i)(prod|production|live|shared|authority|central_pms)') {
    throw "Refusing non-IST or production-like database name: $DatabaseName"
}
foreach ($value in @($ContainerName, $DatabaseName, $DatabaseUser)) {
    if ($value -notmatch '^[A-Za-z0-9_.-]+$') {
        throw 'Container, database, and user identifiers may contain only letters, numbers, dot, underscore, and hyphen.'
    }
}

function Quote-Identifier([string] $Value) {
    return '"' + $Value.Replace('"', '""') + '"'
}

function Get-StateColumnDefinition([string] $State, [string] $ColumnName) {
    $match = [regex]::Match($State, "(?mi)^\s*$([regex]::Escape($ColumnName))\s+([^,\r\n]+),\s*$")
    if (-not $match.Success) { throw "Canonical state does not define column $ColumnName." }
    return "$ColumnName $($match.Groups[1].Value.Trim())"
}

function Get-StateConstraintDefinition([string] $State, [string] $ConstraintName) {
    $escaped = [regex]::Escape($ConstraintName)
    $match = [regex]::Match(
        $State,
        "(?ms)^\s*(?:ADD\s+)?CONSTRAINT\s+$escaped\s+(.+?)(?=,\r?\n\s+(?:ADD\s+)?CONSTRAINT|;\s*$|\r?\n\);)")
    if (-not $match.Success) { throw "Canonical state does not define constraint $ConstraintName." }
    return ($match.Groups[1].Value.Trim() -replace ',\s*$', '')
}

function Get-StateFunctionDefinition([string] $State, [string] $QualifiedName) {
    $pattern = '(?ms)^\s*CREATE\s+OR\s+REPLACE\s+FUNCTION\s+' +
        [regex]::Escape($QualifiedName) + '\s*\(\s*\).*?^\s*\$\$;\s*'
    $match = [regex]::Match($State, $pattern)
    if (-not $match.Success) { throw "Canonical state does not define function $QualifiedName()." }
    return $match.Value.Trim()
}

function Get-StateFunctionComment([string] $State, [string] $QualifiedName) {
    $pattern = '(?mi)^\s*COMMENT\s+ON\s+FUNCTION\s+' +
        [regex]::Escape($QualifiedName) + "\s*\(\s*\)\s+IS\s+'(?<comment>(?:''|[^'])*)';\s*$"
    $match = [regex]::Match($State, $pattern)
    if (-not $match.Success) { throw "Canonical state does not define a comment for function $QualifiedName()." }
    return [pscustomobject]@{
        statement = $match.Value.Trim()
        value = $match.Groups['comment'].Value.Replace("''", "'")
    }
}

function Get-StateTriggerDefinition([string] $State, [string] $TriggerName) {
    $pattern = '(?ms)^\s*CREATE\s+OR\s+REPLACE\s+TRIGGER\s+' +
        [regex]::Escape($TriggerName) + '.*?;\s*'
    $match = [regex]::Match($State, $pattern)
    if (-not $match.Success) { throw "Canonical state does not define trigger $TriggerName." }
    return $match.Value.Trim()
}

function Get-FunctionBody([string] $FunctionDefinition) {
    $match = [regex]::Match($FunctionDefinition, '(?ms)\bAS\s+\$(?:[A-Za-z_][A-Za-z0-9_]*)?\$(?<body>.*?)\$(?:[A-Za-z_][A-Za-z0-9_]*)?\$\s*;?\s*$')
    if (-not $match.Success) { throw 'Could not extract the PL/pgSQL body from a function definition.' }
    return $match.Groups['body'].Value
}

function Normalize-Text([string] $Value) {
    $normalized = $Value.Replace("`r`n", "`n").Replace("`r", "`n")
    $lines = @($normalized -split "`n" | ForEach-Object { $_.TrimEnd() })
    return ($lines -join "`n").Trim()
}

function Normalize-TriggerDefinition([string] $Value) {
    return ((Normalize-Text $Value).ToLowerInvariant() `
        -replace '^create\s+or\s+replace\s+trigger', 'create trigger' `
        -replace '"', '' `
        -replace '\s+', '' `
        -replace ';$', '')
}

function Normalize-CheckConstraintDefinition([string] $Value) {
    $normalized = (Normalize-Text $Value).ToLowerInvariant()
    $semanticVersionInPattern = "semantic_hash_version\s+in\s+\(\s*('(?:''|[^'])*')\s*,\s*('(?:''|[^'])*')\s*\)"
    $normalized = [regex]::Replace(
        $normalized,
        $semanticVersionInPattern,
        'semantic_hash_version = any (array[$1,$2])')
    return ($normalized -replace '::text', '' -replace '[\s"()]', '')
}

function Get-Sha256([string] $Value) {
    $bytes = [Text.Encoding]::UTF8.GetBytes($Value)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        $hash = $algorithm.ComputeHash($bytes)
        return ([BitConverter]::ToString($hash) -replace '-', '').ToLowerInvariant()
    }
    finally { $algorithm.Dispose() }
}

$canonicalFunctionDefinition = Get-StateFunctionDefinition $reportingPeriodAssignmentState $reportingFunction
$canonicalFunctionComment = Get-StateFunctionComment $reportingPeriodAssignmentState $reportingFunction
$canonicalTriggerDefinition = Get-StateTriggerDefinition $reportingPeriodAssignmentState $reportingTrigger
$canonicalFunctionBody = Normalize-Text (Get-FunctionBody $canonicalFunctionDefinition)
$canonicalFunctionContract = [ordered]@{
    identity = "$reportingFunction()"
    returns = 'trigger'
    language = 'plpgsql'
    volatility = 'VOLATILE'
    security_definer = $false
    body = $canonicalFunctionBody
    comment = $canonicalFunctionComment.value
}
$canonicalFunctionContractJson = $canonicalFunctionContract | ConvertTo-Json -Compress
$canonicalTriggerContract = [ordered]@{
    normalized_definition = Normalize-TriggerDefinition $canonicalTriggerDefinition
    enabled = 'O'
    function_identity = "$reportingFunction()"
}
$canonicalTriggerContractJson = $canonicalTriggerContract | ConvertTo-Json -Compress
$canonicalRequiredConstraintDefinition = Get-StateConstraintDefinition $electronicJournalState $canonicalRequiredConstraint
$canonicalRequiredConstraintNormalized = Normalize-CheckConstraintDefinition $canonicalRequiredConstraintDefinition

function Invoke-Psql([string] $Sql, [switch] $Scalar) {
    $arguments = "exec -i $ContainerName psql -X -v ON_ERROR_STOP=1 -U $DatabaseUser -d $DatabaseName"
    if ($Scalar) { $arguments += ' -qAt' } else { $arguments += ' -q' }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = [Diagnostics.ProcessStartInfo]@{
        FileName = 'docker.exe'; Arguments = $arguments; UseShellExecute = $false
        RedirectStandardInput = $true; RedirectStandardOutput = $true; RedirectStandardError = $true
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
        if ($process.ExitCode -ne 0) { throw "POS state reconciliation SQL failed: $stderr" }
        return $stdout
    }
    finally { $process.Dispose() }
}

function Test-ColumnExists([string] $TableName, [string] $ColumnName) {
    return (Invoke-Psql "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='pos' AND table_name='$TableName' AND column_name='$ColumnName');" -Scalar) -eq 't'
}

function Test-ConstraintExists([string] $TableName, [string] $ConstraintName) {
    return (Invoke-Psql "SELECT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='pos.$TableName'::regclass AND conname='$ConstraintName');" -Scalar) -eq 't'
}

function Get-DatabaseObjectInventoryDrift {
    $actualTables = @((Invoke-Psql "SELECT 'pos.' || table_name FROM information_schema.tables WHERE table_schema='pos' AND table_type='BASE TABLE' ORDER BY table_name;" -Scalar) -split "`n" | Where-Object { $_ })
    $actualFunctions = @((Invoke-Psql "SELECT DISTINCT 'pos.' || p.proname FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='pos' AND p.prokind='f' ORDER BY 1;" -Scalar) -split "`n" | Where-Object { $_ })
    $actualTriggers = @((Invoke-Psql "SELECT 'pos.' || c.relname || '.' || t.tgname FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='pos' AND NOT t.tgisinternal ORDER BY 1;" -Scalar) -split "`n" | Where-Object { $_ })
    $findings = [Collections.Generic.List[string]]::new()
    foreach ($scope in @(
        [pscustomobject]@{ kind='table'; expected=@($expectedInventory.expected_tables); actual=$actualTables },
        [pscustomobject]@{ kind='function'; expected=@($expectedInventory.expected_functions); actual=$actualFunctions },
        [pscustomobject]@{ kind='trigger'; expected=@($expectedInventory.expected_triggers); actual=$actualTriggers }
    )) {
        foreach ($missing in @($scope.expected | Where-Object { $_ -notin $scope.actual } | Sort-Object)) {
            $findings.Add("missing unexpected $($scope.kind): $missing")
        }
        foreach ($extra in @($scope.actual | Where-Object { $_ -notin $scope.expected } | Sort-Object)) {
            $findings.Add("extra unexpected $($scope.kind): $extra")
        }
    }
    return @($findings)
}

function Get-ReportingPeriodContractInventory {
    $sql = @'
SELECT json_build_object(
  'function', (
    SELECT json_build_object(
      'oid', p.oid,
      'identity', p.oid::regprocedure::text,
      'definition', pg_get_functiondef(p.oid),
      'source', p.prosrc,
      'language', l.lanname,
      'volatility', CASE p.provolatile WHEN 'i' THEN 'IMMUTABLE' WHEN 's' THEN 'STABLE' ELSE 'VOLATILE' END,
      'security_definer', p.prosecdef,
      'owner', r.rolname,
      'comment', obj_description(p.oid, 'pg_proc'))
    FROM pg_proc p
    JOIN pg_namespace n ON n.oid=p.pronamespace
    JOIN pg_language l ON l.oid=p.prolang
    JOIN pg_roles r ON r.oid=p.proowner
    WHERE n.nspname='pos'
      AND p.proname='validate_fiscal_document_reporting_period_assignment'
      AND p.pronargs=0),
  'trigger', (
    SELECT json_build_object(
      'oid', t.oid,
      'definition', pg_get_triggerdef(t.oid, true),
      'enabled', t.tgenabled,
      'function_identity', t.tgfoid::regprocedure::text,
      'internal', t.tgisinternal)
    FROM pg_trigger t
    WHERE t.tgrelid='pos.fiscal_documents'::regclass
      AND t.tgname='trg_fiscal_documents_reporting_period_assignment')
)::text;
'@
    $live = Invoke-Psql $sql -Scalar | ConvertFrom-Json
    $liveFunctionContract = $null
    $liveFunctionContractJson = $null
    if ($null -ne $live.function) {
        $liveFunctionContract = [ordered]@{
            identity = [string]$live.function.identity
            returns = 'trigger'
            language = [string]$live.function.language
            volatility = [string]$live.function.volatility
            security_definer = [bool]$live.function.security_definer
            body = Normalize-Text ([string]$live.function.source)
            comment = [string]$live.function.comment
        }
        $liveFunctionContractJson = $liveFunctionContract | ConvertTo-Json -Compress
    }
    $liveTriggerContract = $null
    $liveTriggerContractJson = $null
    if ($null -ne $live.trigger) {
        $liveTriggerContract = [ordered]@{
            normalized_definition = Normalize-TriggerDefinition ([string]$live.trigger.definition)
            enabled = [string]$live.trigger.enabled
            function_identity = [string]$live.trigger.function_identity
        }
        $liveTriggerContractJson = $liveTriggerContract | ConvertTo-Json -Compress
    }
    $functionAligned = $null -ne $liveFunctionContractJson -and $liveFunctionContractJson -ceq $canonicalFunctionContractJson
    $triggerAligned = $null -ne $liveTriggerContractJson -and $liveTriggerContractJson -ceq $canonicalTriggerContractJson
    $scope = if ($functionAligned -and $triggerAligned) { 'ALIGNED' }
        elseif (-not $functionAligned -and $triggerAligned) { 'FUNCTION_ONLY' }
        else { 'FUNCTION_AND_TRIGGER' }
    return [pscustomobject]@{
        function_definition_aligned = $functionAligned
        trigger_definition_aligned = $triggerAligned
        function_drift_scope = $scope
        apply_would_replace_function = -not $functionAligned
        apply_would_replace_trigger = -not $triggerAligned
        canonical_function_normalized_sha256 = Get-Sha256 $canonicalFunctionContractJson
        live_function_normalized_sha256 = if ($null -eq $liveFunctionContractJson) { $null } else { Get-Sha256 $liveFunctionContractJson }
        canonical_function_source_sha256 = Get-Sha256 (Normalize-Text $canonicalFunctionDefinition)
        live_pg_get_functiondef_sha256 = if ($null -eq $live.function) { $null } else { Get-Sha256 (Normalize-Text ([string]$live.function.definition)) }
        canonical_trigger_normalized_sha256 = Get-Sha256 $canonicalTriggerContractJson
        live_trigger_normalized_sha256 = if ($null -eq $liveTriggerContractJson) { $null } else { Get-Sha256 $liveTriggerContractJson }
        canonical_function_definition = $canonicalFunctionDefinition
        canonical_function_comment = $canonicalFunctionComment.value
        live_function = $live.function
        canonical_trigger_definition = $canonicalTriggerDefinition
        live_trigger = $live.trigger
    }
}

function Get-AllTableRowCounts {
    $tableText = Invoke-Psql "SELECT table_name FROM information_schema.tables WHERE table_schema='pos' AND table_type='BASE TABLE' ORDER BY table_name;" -Scalar
    $counts = [ordered]@{}
    foreach ($table in @($tableText -split "`n")) {
        $name = $table.Trim()
        if ($name -notmatch '^[a-z][a-z0-9_]*$') { throw "Unsafe POS table identifier: $name" }
        $counts[$name] = [int64](Invoke-Psql "SELECT count(*) FROM pos.$name;" -Scalar)
    }
    return $counts
}

function Get-ProtectedManifest {
    $sql = @'
SELECT json_build_object(
  'fiscal_document_rows', (SELECT count(*) FROM pos.fiscal_documents),
  'fiscal_document_ids', (SELECT coalesce(json_agg(fiscal_document_id ORDER BY fiscal_document_id), '[]') FROM pos.fiscal_documents),
  'fiscal_document_id_hash', (SELECT md5(coalesce(string_agg(fiscal_document_id::text, ',' ORDER BY fiscal_document_id), '')) FROM pos.fiscal_documents),
  'fiscal_document_number_hash', (SELECT md5(coalesce(string_agg(coalesce(fiscal_document_number, '<null>'), ',' ORDER BY fiscal_document_id), '')) FROM pos.fiscal_documents),
  'fiscal_document_assignment_hash', (SELECT md5(coalesce(string_agg(concat_ws('|', fiscal_document_id, fiscal_sequence_policy_id, fiscal_sequence_value, fiscal_document_number, business_day_date, fiscal_reporting_period_id), ',' ORDER BY fiscal_document_id), '')) FROM pos.fiscal_documents),
  'idempotency_rows', (SELECT count(*) FROM pos.idempotency_records),
  'idempotency_id_hash', (SELECT md5(coalesce(string_agg(idempotency_record_id::text, ',' ORDER BY idempotency_record_id), '')) FROM pos.idempotency_records),
  'sequence_rows', (SELECT count(*) FROM pos.fiscal_sequence_states),
  'sequence_ids', (SELECT coalesce(json_agg(fiscal_sequence_state_id ORDER BY fiscal_sequence_state_id), '[]') FROM pos.fiscal_sequence_states),
  'sequence_states', (SELECT coalesce(json_agg(json_build_object(
      'fiscal_sequence_state_id', fiscal_sequence_state_id,
      'fiscal_sequence_policy_id', fiscal_sequence_policy_id,
      'current_sequence_value', current_sequence_value,
      'last_reserved_sequence_value', last_reserved_sequence_value,
      'last_issued_sequence_value', last_issued_sequence_value,
      'sequence_state_code_id', sequence_state_code_id,
      'last_transition_at', last_transition_at) ORDER BY fiscal_sequence_state_id), '[]') FROM pos.fiscal_sequence_states),
  'sequence_state_hash', (SELECT md5(coalesce(string_agg(concat_ws('|', fiscal_sequence_state_id, fiscal_sequence_policy_id, current_sequence_value, last_reserved_sequence_value, last_issued_sequence_value, sequence_state_code_id, last_transition_at), ',' ORDER BY fiscal_sequence_state_id), '')) FROM pos.fiscal_sequence_states),
  'reporting_period_rows', (SELECT count(*) FROM pos.fiscal_reporting_periods),
  'reporting_period_ids', (SELECT coalesce(json_agg(fiscal_reporting_period_id ORDER BY fiscal_reporting_period_id), '[]') FROM pos.fiscal_reporting_periods),
  'reporting_periods', (SELECT coalesce(json_agg(json_build_object(
      'fiscal_reporting_period_id', fiscal_reporting_period_id,
      'business_day_date', business_day_date,
      'period_status_code_id', period_status_code_id,
      'period_start_at', period_start_at,
      'period_end_at', period_end_at,
      'closed_at', closed_at) ORDER BY fiscal_reporting_period_id), '[]') FROM pos.fiscal_reporting_periods),
  'reporting_period_state_hash', (SELECT md5(coalesce(string_agg(concat_ws('|', fiscal_reporting_period_id, business_day_date, period_status_code_id, period_start_at, period_end_at, closed_at), ',' ORDER BY fiscal_reporting_period_id), '')) FROM pos.fiscal_reporting_periods),
  'header_snapshot_rows', (SELECT count(*) FROM pos.fiscal_document_header_snapshots),
  'header_snapshot_ids', (SELECT coalesce(json_agg(fiscal_document_header_snapshot_id ORDER BY fiscal_document_header_snapshot_id), '[]') FROM pos.fiscal_document_header_snapshots),
  'header_snapshot_id_hash', (SELECT md5(coalesce(string_agg(fiscal_document_header_snapshot_id::text, ',' ORDER BY fiscal_document_header_snapshot_id), '')) FROM pos.fiscal_document_header_snapshots),
  'electronic_journal_rows', (SELECT count(*) FROM pos.electronic_journal_records),
  'electronic_journal_ids', (SELECT coalesce(json_agg(electronic_journal_record_id ORDER BY electronic_journal_record_id), '[]') FROM pos.electronic_journal_records),
  'electronic_journal_records', (SELECT coalesce(json_agg(json_build_object(
      'electronic_journal_record_id', electronic_journal_record_id,
      'electronic_journal_stream_id', electronic_journal_stream_id,
      'event_reference', event_reference,
      'stream_sequence_value', stream_sequence_value,
      'semantic_hash_version', semantic_hash_version,
      'semantic_hash', semantic_hash,
      'integrity_hash_version', integrity_hash_version,
      'integrity_hash', integrity_hash,
      'previous_integrity_hash', previous_integrity_hash,
      'journal_context', journal_context,
      'printable_sales_invoice_text', to_jsonb(e) ->> 'printable_sales_invoice_text') ORDER BY electronic_journal_record_id), '[]') FROM pos.electronic_journal_records e),
  'electronic_journal_id_hash', (SELECT md5(coalesce(string_agg(electronic_journal_record_id::text, ',' ORDER BY electronic_journal_record_id), '')) FROM pos.electronic_journal_records),
  'ej_canonical_rows', (SELECT count(*) FROM pos.electronic_journal_records WHERE is_canonical),
  'ej_legacy_rows', (SELECT count(*) FROM pos.electronic_journal_records WHERE NOT is_canonical),
  'ej_semantic_hash', (SELECT md5(coalesce(string_agg(coalesce(semantic_hash, '<null>'), ',' ORDER BY electronic_journal_record_id), '')) FROM pos.electronic_journal_records),
  'ej_semantic_hash_version_hash', (SELECT md5(coalesce(string_agg(coalesce(semantic_hash_version, '<null>'), ',' ORDER BY electronic_journal_record_id), '')) FROM pos.electronic_journal_records),
  'ej_integrity_hash', (SELECT md5(coalesce(string_agg(coalesce(integrity_hash, '<null>'), ',' ORDER BY electronic_journal_record_id), '')) FROM pos.electronic_journal_records),
  'ej_integrity_hash_version_hash', (SELECT md5(coalesce(string_agg(coalesce(integrity_hash_version, '<null>'), ',' ORDER BY electronic_journal_record_id), '')) FROM pos.electronic_journal_records),
  'ej_previous_integrity_hash', (SELECT md5(coalesce(string_agg(coalesce(previous_integrity_hash, '<null>'), ',' ORDER BY electronic_journal_record_id), '')) FROM pos.electronic_journal_records),
  'ej_journal_context_nonnull_rows', (SELECT count(*) FROM pos.electronic_journal_records WHERE journal_context IS NOT NULL),
  'ej_journal_context_hash', (SELECT md5(coalesce(string_agg(coalesce(journal_context::text, '<null>'), ',' ORDER BY electronic_journal_record_id), '')) FROM pos.electronic_journal_records),
  'ej_stream_rows', (SELECT count(*) FROM pos.electronic_journal_streams),
  'ej_stream_ids', (SELECT coalesce(json_agg(electronic_journal_stream_id ORDER BY electronic_journal_stream_id), '[]') FROM pos.electronic_journal_streams),
  'ej_stream_heads', (SELECT coalesce(json_agg(json_build_object(
      'electronic_journal_stream_id', electronic_journal_stream_id,
      'last_sequence_value', last_sequence_value,
      'last_event_hash', last_event_hash) ORDER BY electronic_journal_stream_id), '[]') FROM pos.electronic_journal_streams),
  'ej_stream_head_hash', (SELECT md5(coalesce(string_agg(concat_ws('|', electronic_journal_stream_id, last_sequence_value, last_event_hash), ',' ORDER BY electronic_journal_stream_id), '')) FROM pos.electronic_journal_streams),
  'x_z_report_rows', (SELECT count(*) FROM pos.x_z_reports),
  'x_z_report_ids', (SELECT coalesce(json_agg(x_z_report_id ORDER BY x_z_report_id), '[]') FROM pos.x_z_reports),
  'bir_report_rows', (SELECT count(*) FROM pos.bir_sales_summary_reports),
  'bir_report_ids', (SELECT coalesce(json_agg(bir_sales_summary_report_id ORDER BY bir_sales_summary_report_id), '[]') FROM pos.bir_sales_summary_reports)
)::text;
'@
    return (Invoke-Psql $sql -Scalar | ConvertFrom-Json)
}

function Get-CanonicalRequiredConstraintInventory {
    $sql = @"
SELECT json_build_object(
  'definition', (
    SELECT pg_get_constraintdef(c.oid, true)
    FROM pg_constraint c
    WHERE c.conrelid='pos.electronic_journal_records'::regclass
      AND c.conname='$canonicalRequiredConstraint'),
  'legacy_v1_rows', (SELECT count(*) FROM pos.electronic_journal_records
    WHERE is_canonical AND semantic_hash_version='$legacyEjSemanticHashVersion'),
  'current_v2_rows', (SELECT count(*) FROM pos.electronic_journal_records
    WHERE is_canonical AND semantic_hash_version='$currentEjSemanticHashVersion'),
  'unsupported_rows', (SELECT count(*) FROM pos.electronic_journal_records
    WHERE is_canonical AND semantic_hash_version NOT IN ('$legacyEjSemanticHashVersion','$currentEjSemanticHashVersion')),
  'unsupported_versions', (SELECT coalesce(json_agg(q.semantic_hash_version ORDER BY q.semantic_hash_version), '[]')
    FROM (SELECT DISTINCT semantic_hash_version FROM pos.electronic_journal_records
          WHERE is_canonical AND semantic_hash_version NOT IN ('$legacyEjSemanticHashVersion','$currentEjSemanticHashVersion')) q)
)::text;
"@
    $inventory = Invoke-Psql $sql -Scalar | ConvertFrom-Json
    $normalizedDefinition = if ($null -eq $inventory.definition) { $null } else {
        Normalize-CheckConstraintDefinition ([string]$inventory.definition)
    }
    return [pscustomobject]@{
        constraint_present = $null -ne $inventory.definition
        definition_aligned = $null -ne $normalizedDefinition -and $normalizedDefinition -ceq $canonicalRequiredConstraintNormalized
        definition = $inventory.definition
        normalized_sha256 = if ($null -eq $normalizedDefinition) { $null } else { Get-Sha256 $normalizedDefinition }
        canonical_definition = $canonicalRequiredConstraintDefinition
        canonical_normalized_sha256 = Get-Sha256 $canonicalRequiredConstraintNormalized
        legacy_v1_rows = [int64]$inventory.legacy_v1_rows
        current_v2_rows = [int64]$inventory.current_v2_rows
        unsupported_rows = [int64]$inventory.unsupported_rows
        unsupported_versions = @($inventory.unsupported_versions)
        apply_would_replace = $null -ne $inventory.definition -and $normalizedDefinition -cne $canonicalRequiredConstraintNormalized
        apply_would_create = $null -eq $inventory.definition
    }
}

function Get-AncestryInventory {
    $sql = @'
SELECT coalesce(json_agg(row_to_json(q) ORDER BY q.document_number, q.fiscal_document_id), '[]')::text
FROM (
  SELECT d.fiscal_document_id, d.fiscal_document_number AS document_number,
         document_type.code_key AS document_type,
         d.central_pms_payment_attempt_ref AS payment_attempt_ref,
         d.central_pms_payment_confirmation_ref AS payment_confirmation_ref,
         d.payment_finality_ref,
         d.document_context ->> 'upstream_finality_ref' AS context_completion_authority,
         idempotency.idempotency_context ->> 'upstream_finality_ref' AS idempotency_completion_authority,
         d.document_context -> 'fiscal_tenders' -> 0 ->> 'payment_finality_ref' AS tender_completion_authority,
         statutory.final_payable_amount_minor_units,
         (statutory.final_payable_amount_minor_units = 0 AND d.payment_finality_ref IS NULL) AS zero_payable_statutory_candidate,
         CASE WHEN d.payment_finality_ref IS NOT NULL AND btrim(d.payment_finality_ref) <> ''
                    AND d.payment_finality_ref = d.document_context ->> 'upstream_finality_ref'
                    AND d.payment_finality_ref = idempotency.idempotency_context ->> 'upstream_finality_ref'
                    AND d.payment_finality_ref = d.document_context -> 'fiscal_tenders' -> 0 ->> 'payment_finality_ref'
              THEN 'PAYMENT_FINALITY' END AS proposed_completion_basis,
         CASE WHEN d.payment_finality_ref IS NOT NULL AND btrim(d.payment_finality_ref) <> ''
                    AND d.payment_finality_ref = d.document_context ->> 'upstream_finality_ref'
                    AND d.payment_finality_ref = idempotency.idempotency_context ->> 'upstream_finality_ref'
                    AND d.payment_finality_ref = d.document_context -> 'fiscal_tenders' -> 0 ->> 'payment_finality_ref'
              THEN d.payment_finality_ref END AS proposed_completion_authority_ref,
         'pos.fiscal_documents.payment_finality_ref corroborated by document_context, fiscal_tender snapshot, and idempotency_context' AS derivation_source
  FROM pos.fiscal_documents d
  JOIN pos.controlled_codes document_type ON document_type.controlled_code_id=d.fiscal_document_type_code_id
  LEFT JOIN pos.idempotency_records idempotency ON idempotency.linked_fiscal_document_id=d.fiscal_document_id
  LEFT JOIN pos.fiscal_document_applied_statutory_facts statutory ON statutory.fiscal_document_id=d.fiscal_document_id
) q;
'@
    return @((Invoke-Psql $sql -Scalar | ConvertFrom-Json))
}

function Get-SchemaDrift {
    $sql = @'
WITH expected_columns(table_name,column_name,expected_type,expected_nullable) AS (
  VALUES ('fiscal_documents','completion_basis','character varying(64)','NO'),
         ('fiscal_documents','completion_authority_ref','text','YES'),
         ('electronic_journal_records','printable_sales_invoice_text','text','YES')
), actual_columns AS (
  SELECT table_name,column_name,
         CASE WHEN data_type='character varying' THEN data_type || '(' || character_maximum_length || ')' ELSE data_type END AS actual_type,
         is_nullable
  FROM information_schema.columns WHERE table_schema='pos'
), expected_constraints(table_name,constraint_name) AS (
  VALUES ('fiscal_documents','ck_fiscal_documents__completion_basis'),
         ('fiscal_documents','ck_fiscal_documents__completion_ancestry'),
         ('electronic_journal_records','ck_ej_records__printable_sales_invoice'),
         ('electronic_journal_records','ck_ej_records__canonical_required')
)
SELECT json_build_object(
  'missing_columns', (SELECT coalesce(json_agg(e.table_name || '.' || e.column_name ORDER BY e.table_name,e.column_name), '[]') FROM expected_columns e LEFT JOIN actual_columns a USING(table_name,column_name) WHERE a.column_name IS NULL),
  'mismatched_types', (SELECT coalesce(json_agg(json_build_object('column',e.table_name || '.' || e.column_name,'expected',e.expected_type,'actual',a.actual_type) ORDER BY e.table_name,e.column_name), '[]') FROM expected_columns e JOIN actual_columns a USING(table_name,column_name) WHERE a.actual_type<>e.expected_type),
  'mismatched_nullability', (SELECT coalesce(json_agg(json_build_object('column',e.table_name || '.' || e.column_name,'expected',e.expected_nullable,'actual',a.is_nullable) ORDER BY e.table_name,e.column_name), '[]') FROM expected_columns e JOIN actual_columns a USING(table_name,column_name) WHERE a.is_nullable<>e.expected_nullable),
  'missing_constraints', (SELECT coalesce(json_agg(e.table_name || '.' || e.constraint_name ORDER BY e.table_name,e.constraint_name), '[]') FROM expected_constraints e LEFT JOIN pg_constraint c ON c.conrelid=('pos.' || e.table_name)::regclass AND c.conname=e.constraint_name WHERE c.oid IS NULL),
  'extra_legacy_compatible_objects', json_build_array(),
  'unexpected_drift', json_build_array()
)::text;
'@
    $result = Invoke-Psql $sql -Scalar | ConvertFrom-Json
    $canonicalRequired = Get-CanonicalRequiredConstraintInventory
    $result | Add-Member -NotePropertyName mismatched_constraints -NotePropertyValue @(
        if ($canonicalRequired.constraint_present -and -not $canonicalRequired.definition_aligned) {
            'electronic_journal_records.ck_ej_records__canonical_required'
        }
    )
    $result.unexpected_drift = @(Get-DatabaseObjectInventoryDrift)
    return $result
}

function Get-PrintableFacts {
    if (-not (Test-ColumnExists 'electronic_journal_records' $printableColumn)) {
        return [pscustomobject]@{ column_present = $false; null_rows = $null; nonnull_rows = $null }
    }
    return (Invoke-Psql "SELECT json_build_object('column_present',true,'null_rows',count(*) FILTER (WHERE printable_sales_invoice_text IS NULL),'nonnull_rows',count(*) FILTER (WHERE printable_sales_invoice_text IS NOT NULL))::text FROM pos.electronic_journal_records;" -Scalar | ConvertFrom-Json)
}

function Write-Evidence([object] $Value) {
    if ([string]::IsNullOrWhiteSpace($EvidenceDir)) { return }
    [void](New-Item -ItemType Directory -Path $EvidenceDir -Force)
    $path = Join-Path $EvidenceDir 'pos-persistent-state-reconciliation.json'
    [IO.File]::WriteAllText($path, ($Value | ConvertTo-Json -Depth 20), [Text.UTF8Encoding]::new($false))
}

$before = [ordered]@{
    table_row_counts = Get-AllTableRowCounts
    protected_manifest = Get-ProtectedManifest
    ancestry_inventory = @(Get-AncestryInventory)
    printable_facts = Get-PrintableFacts
    canonical_required_constraint = Get-CanonicalRequiredConstraintInventory
    reporting_period_assignment = Get-ReportingPeriodContractInventory
}
$inspection = [ordered]@{
    mode = $Mode; container = $ContainerName; database = $DatabaseName
    canonical_fiscal_document_state_sha256 = (Get-FileHash $fiscalDocumentStatePath -Algorithm SHA256).Hash.ToLowerInvariant()
    canonical_electronic_journal_state_sha256 = (Get-FileHash $electronicJournalStatePath -Algorithm SHA256).Hash.ToLowerInvariant()
    canonical_reporting_period_assignment_state_sha256 = (Get-FileHash $reportingPeriodAssignmentStatePath -Algorithm SHA256).Hash.ToLowerInvariant()
    expected_inventory_sha256 = (Get-FileHash $expectedInventoryPath -Algorithm SHA256).Hash.ToLowerInvariant()
    schema_drift_inventory = Get-SchemaDrift
    before = $before; result = 'INSPECTED'
}
$unresolvedAncestry = @($before.ancestry_inventory | Where-Object {
    [string]::IsNullOrWhiteSpace($_.proposed_completion_basis) -or [string]::IsNullOrWhiteSpace($_.proposed_completion_authority_ref)
})
$zeroPayableCandidates = @($before.ancestry_inventory | Where-Object { $_.zero_payable_statutory_candidate -eq $true })
$inspection.unresolved_historical_rows = $unresolvedAncestry.Count
$inspection.zero_payable_historical_rows = $zeroPayableCandidates.Count

if ($Mode -eq 'Inspect') {
    Write-Evidence $inspection
    $inspection | ConvertTo-Json -Depth 20
    return
}
$unexpectedDrift = @($inspection.schema_drift_inventory.unexpected_drift)
if ($unexpectedDrift.Count -gt 0) {
    $inspection.result = 'BLOCKED_UNEXPECTED_DATABASE_INVENTORY_DRIFT'; Write-Evidence $inspection
    throw "BLOCKED_UNEXPECTED_DATABASE_INVENTORY_DRIFT: $($unexpectedDrift.Count) unrelated database object inventory finding(s) require separate review before Apply."
}
if ($unresolvedAncestry.Count -gt 0) {
    $inspection.result = 'BLOCKED_NONDETERMINISTIC_COMPLETION_ANCESTRY_BACKFILL'; Write-Evidence $inspection
    throw "BLOCKED_NONDETERMINISTIC_COMPLETION_ANCESTRY_BACKFILL: $($unresolvedAncestry.Count) historical fiscal document row(s) lack independently corroborated immutable payment-finality ancestry."
}
if ($zeroPayableCandidates.Count -gt 0) {
    $inspection.result = 'BLOCKED_ZERO_PAYABLE_STATUTORY_ANCESTRY_REQUIRES_EXPLICIT_MAPPING'; Write-Evidence $inspection
    throw "BLOCKED_ZERO_PAYABLE_STATUTORY_ANCESTRY_REQUIRES_EXPLICIT_MAPPING: $($zeroPayableCandidates.Count) historical zero-payable candidate row(s) require a separately governed authoritative mapping."
}
if ($before.printable_facts.column_present -and [int64]$before.printable_facts.nonnull_rows -gt 0) {
    $inspection.result = 'BLOCKED_PREEXISTING_EJ_PRINTABLE_TEXT'; Write-Evidence $inspection
    throw 'BLOCKED_PREEXISTING_EJ_PRINTABLE_TEXT: existing printable text requires separate immutable-source review.'
}
if ([int64]$before.canonical_required_constraint.unsupported_rows -gt 0) {
    $inspection.result = 'BLOCKED_UNSUPPORTED_HISTORICAL_EJ_SEMANTIC_VERSION'; Write-Evidence $inspection
    throw "BLOCKED_UNSUPPORTED_HISTORICAL_EJ_SEMANTIC_VERSION: $($before.canonical_required_constraint.unsupported_rows) canonical historical EJ row(s) use unsupported semantic version(s)."
}
if (-not $PSCmdlet.ShouldProcess("$ContainerName/$DatabaseName", 'Additively reconcile canonical POS persistent IST state')) { return }

$sql = [Text.StringBuilder]::new()
[void]$sql.AppendLine('\set ON_ERROR_STOP on')
[void]$sql.AppendLine('BEGIN;')
$completionBasisDefinition = (Get-StateColumnDefinition $fiscalDocumentState 'completion_basis') -replace '(?i)\s+NOT\s+NULL', '' -replace "(?i)\s+DEFAULT\s+'[^']+'", ''
[void]$sql.AppendLine("ALTER TABLE pos.fiscal_documents ADD COLUMN IF NOT EXISTS $completionBasisDefinition;")
[void]$sql.AppendLine("ALTER TABLE pos.fiscal_documents ADD COLUMN IF NOT EXISTS $(Get-StateColumnDefinition $fiscalDocumentState 'completion_authority_ref');")
[void]$sql.AppendLine("ALTER TABLE pos.electronic_journal_records ADD COLUMN IF NOT EXISTS $(Get-StateColumnDefinition $electronicJournalState $printableColumn);")
[void]$sql.AppendLine(@'
DO $reconcile$
BEGIN
  IF EXISTS (SELECT 1 FROM pos.fiscal_documents
             WHERE completion_basis IS NOT NULL AND completion_basis <> 'PAYMENT_FINALITY'
                OR completion_authority_ref IS NOT NULL AND completion_authority_ref <> payment_finality_ref) THEN
    RAISE EXCEPTION 'existing completion columns contradict proven payment-finality ancestry';
  END IF;
END
$reconcile$;

UPDATE pos.fiscal_documents
SET completion_basis = COALESCE(completion_basis, 'PAYMENT_FINALITY'),
    completion_authority_ref = COALESCE(completion_authority_ref, payment_finality_ref)
WHERE completion_basis IS NULL OR completion_authority_ref IS NULL;

DO $reconcile$
BEGIN
  IF EXISTS (SELECT 1 FROM pos.fiscal_documents
             WHERE completion_basis IS NULL OR completion_authority_ref IS NULL
                OR completion_basis <> 'PAYMENT_FINALITY' OR completion_authority_ref <> payment_finality_ref) THEN
    RAISE EXCEPTION 'completion ancestry reconciliation left unresolved or contradictory rows';
  END IF;
  IF EXISTS (SELECT 1 FROM pos.electronic_journal_records WHERE printable_sales_invoice_text IS NOT NULL) THEN
    RAISE EXCEPTION 'historical Electronic Journal printable text must remain null';
  END IF;
  IF EXISTS (SELECT 1 FROM pos.electronic_journal_records WHERE is_canonical AND journal_context IS NOT NULL) THEN
    RAISE EXCEPTION 'canonical Electronic Journal rows violate legacy-context constraint';
  END IF;
END
$reconcile$;

ALTER TABLE pos.fiscal_documents ALTER COLUMN completion_basis SET NOT NULL;
'@)
foreach ($constraint in $completionConstraints) {
    if (-not (Test-ConstraintExists 'fiscal_documents' $constraint)) {
        [void]$sql.AppendLine("ALTER TABLE pos.fiscal_documents ADD CONSTRAINT $(Quote-Identifier $constraint) $(Get-StateConstraintDefinition $fiscalDocumentState $constraint);")
    }
}
if (-not (Test-ConstraintExists 'electronic_journal_records' $printableConstraint)) {
    [void]$sql.AppendLine("ALTER TABLE pos.electronic_journal_records ADD CONSTRAINT $(Quote-Identifier $printableConstraint) $(Get-StateConstraintDefinition $electronicJournalState $printableConstraint);")
}
if ($before.canonical_required_constraint.apply_would_replace) {
    [void]$sql.AppendLine("ALTER TABLE pos.electronic_journal_records DROP CONSTRAINT $(Quote-Identifier $canonicalRequiredConstraint);")
}
if ($before.canonical_required_constraint.apply_would_replace -or $before.canonical_required_constraint.apply_would_create) {
    [void]$sql.AppendLine("ALTER TABLE pos.electronic_journal_records ADD CONSTRAINT $(Quote-Identifier $canonicalRequiredConstraint) $canonicalRequiredConstraintDefinition;")
}
if ($before.reporting_period_assignment.apply_would_replace_function) {
    [void]$sql.AppendLine($canonicalFunctionDefinition)
    [void]$sql.AppendLine($canonicalFunctionComment.statement)
}
if ($before.reporting_period_assignment.apply_would_replace_trigger) {
    [void]$sql.AppendLine($canonicalTriggerDefinition)
}
[void]$sql.AppendLine('COMMIT;')
[void](Invoke-Psql $sql.ToString())

$after = [ordered]@{
    table_row_counts = Get-AllTableRowCounts
    protected_manifest = Get-ProtectedManifest
    ancestry_inventory = @(Get-AncestryInventory)
    printable_facts = Get-PrintableFacts
    canonical_required_constraint = Get-CanonicalRequiredConstraintInventory
    reporting_period_assignment = Get-ReportingPeriodContractInventory
}
if (($before.protected_manifest | ConvertTo-Json -Depth 20 -Compress) -cne ($after.protected_manifest | ConvertTo-Json -Depth 20 -Compress) -or
    ($before.table_row_counts | ConvertTo-Json -Depth 5 -Compress) -cne ($after.table_row_counts | ConvertTo-Json -Depth 5 -Compress)) {
    throw 'State reconciliation changed protected row counts, identities, sequences, periods, snapshots, or EJ history.'
}
if ([int64]$after.printable_facts.nonnull_rows -ne 0) { throw 'State reconciliation populated historical Electronic Journal printable text.' }
if (-not $after.canonical_required_constraint.definition_aligned -or
    [int64]$after.canonical_required_constraint.unsupported_rows -ne 0) {
    throw 'Persistent database canonical-required constraint does not accept exactly the supported EJ V1/V2 semantic profiles.'
}
if (-not $after.reporting_period_assignment.function_definition_aligned -or
    -not $after.reporting_period_assignment.trigger_definition_aligned) {
    throw 'Persistent database reporting-period assignment function or trigger does not match canonical state after reconciliation.'
}
if ($null -ne $before.reporting_period_assignment.live_function -and
    ($before.reporting_period_assignment.live_function.owner -cne $after.reporting_period_assignment.live_function.owner -or
     [bool]$before.reporting_period_assignment.live_function.security_definer -ne [bool]$after.reporting_period_assignment.live_function.security_definer)) {
    throw 'Reporting-period function reconciliation changed owner or security-definer posture.'
}
$afterDrift = Get-SchemaDrift
if (@($afterDrift.missing_columns).Count -ne 0 -or @($afterDrift.mismatched_types).Count -ne 0 -or
    @($afterDrift.mismatched_nullability).Count -ne 0 -or @($afterDrift.missing_constraints).Count -ne 0 -or
    @($afterDrift.mismatched_constraints).Count -ne 0 -or
    @($afterDrift.unexpected_drift).Count -ne 0) {
    throw 'Persistent database does not match the approved scoped reconciliation state after reconciliation.'
}
$inspection.after = $after
$inspection.after_schema_drift_inventory = $afterDrift
$inspection.result = 'RECONCILED'
Write-Evidence $inspection
$inspection | ConvertTo-Json -Depth 20
