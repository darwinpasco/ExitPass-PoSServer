<#
.SYNOPSIS
Inspects or additively reconciles an existing non-production POS IST database to selected canonical db/state definitions.

.DESCRIPTION
The repository files under db/state remain authoritative. This helper extracts the approved
supplier identity columns and constraints from those files at execution time. It adds missing
columns as nullable first, preserves all rows and identifiers, and refuses to fabricate supplier
facts for approved profiles or immutable fiscal snapshots.
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
$profileStatePath = Join-Path $repoRoot 'db\state\tables\pos.sales_invoice_header_profiles.sql'
$snapshotStatePath = Join-Path $repoRoot 'db\state\tables\pos.fiscal_document_header_snapshots.sql'
$profileState = [IO.File]::ReadAllText($profileStatePath)
$snapshotState = [IO.File]::ReadAllText($snapshotStatePath)
$supplierColumns = @(
    'supplier_developer_registered_name',
    'supplier_developer_address',
    'supplier_developer_tin'
)
$profileConstraints = @(
    'ck_sales_invoice_header_profiles__supplier_name_not_blank',
    'ck_sales_invoice_header_profiles__supplier_address_not_blank',
    'ck_sales_invoice_header_profiles__supplier_tin_not_blank',
    'ck_sales_invoice_header_profiles__approved_completeness'
)
$snapshotConstraints = @(
    'ck_fiscal_document_header_snapshots__supplier_name_not_blank',
    'ck_fiscal_document_header_snapshots__supplier_address_not_blank',
    'ck_fiscal_document_header_snapshots__supplier_tin_not_blank'
)

if ($DatabaseName -notmatch '(?i)(ist|validation|local|test|disposable)' -or
    $DatabaseName -match '(?i)(prod|production|live|shared|authority|central_pms)') {
    throw "Refusing non-IST or production-like database name: $DatabaseName"
}
foreach ($value in @($ContainerName, $DatabaseName, $DatabaseUser)) {
    if ($value -notmatch '^[A-Za-z0-9_.-]+$') {
        throw "Container, database, and user identifiers may contain only letters, numbers, dot, underscore, and hyphen."
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
        "(?ms)^\s*CONSTRAINT\s+$escaped\s+(.+?)(?=,\r?\n\s+CONSTRAINT|\r?\n\);)")
    if (-not $match.Success) { throw "Canonical state does not define constraint $ConstraintName." }
    return ($match.Groups[1].Value.Trim() -replace ',\s*$', '')
}

function Invoke-Psql([string] $Sql, [switch] $Scalar) {
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
        if ($process.ExitCode -ne 0) { throw "POS state reconciliation SQL failed: $stderr" }
        return $stdout
    }
    finally {
        $process.Dispose()
    }
}

function Get-DatabaseFacts {
    $sql = @'
SELECT json_build_object(
  'profile_rows', (SELECT count(*) FROM pos.sales_invoice_header_profiles),
  'profile_id_hash', (SELECT md5(coalesce(string_agg(sales_invoice_header_profile_id::text, ',' ORDER BY sales_invoice_header_profile_id), '')) FROM pos.sales_invoice_header_profiles),
  'snapshot_rows', (SELECT count(*) FROM pos.fiscal_document_header_snapshots),
  'snapshot_id_hash', (SELECT md5(coalesce(string_agg(fiscal_document_header_snapshot_id::text, ',' ORDER BY fiscal_document_header_snapshot_id), '')) FROM pos.fiscal_document_header_snapshots),
  'fiscal_document_rows', (SELECT count(*) FROM pos.fiscal_documents),
  'electronic_journal_rows', (SELECT count(*) FROM pos.electronic_journal_records),
  'supplier_profile_columns', (SELECT count(*) FROM information_schema.columns WHERE table_schema='pos' AND table_name='sales_invoice_header_profiles' AND column_name LIKE 'supplier_developer_%'),
  'supplier_snapshot_columns', (SELECT count(*) FROM information_schema.columns WHERE table_schema='pos' AND table_name='fiscal_document_header_snapshots' AND column_name LIKE 'supplier_developer_%'),
  'supplier_profile_constraints', (SELECT count(*) FROM pg_constraint WHERE conrelid='pos.sales_invoice_header_profiles'::regclass AND (conname LIKE '%supplier_%' OR conname='ck_sales_invoice_header_profiles__approved_completeness')),
  'supplier_snapshot_constraints', (SELECT count(*) FROM pg_constraint WHERE conrelid='pos.fiscal_document_header_snapshots'::regclass AND conname LIKE '%supplier_%')
)::text;
'@
    return (Invoke-Psql $sql -Scalar | ConvertFrom-Json)
}

function Write-Evidence([object] $Value) {
    if ([string]::IsNullOrWhiteSpace($EvidenceDir)) { return }
    [void](New-Item -ItemType Directory -Path $EvidenceDir -Force)
    $path = Join-Path $EvidenceDir 'pos-persistent-state-reconciliation.json'
    [IO.File]::WriteAllText($path, ($Value | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
}

$before = Get-DatabaseFacts
$inspection = [ordered]@{
    mode = $Mode
    database = $DatabaseName
    canonical_profile_state_sha256 = (Get-FileHash $profileStatePath -Algorithm SHA256).Hash.ToLowerInvariant()
    canonical_snapshot_state_sha256 = (Get-FileHash $snapshotStatePath -Algorithm SHA256).Hash.ToLowerInvariant()
    before = $before
    result = 'INSPECTED'
}

if ($Mode -eq 'Inspect') {
    Write-Evidence $inspection
    $inspection | ConvertTo-Json -Depth 8
    return
}

if (-not $PSCmdlet.ShouldProcess("$ContainerName/$DatabaseName", 'Reconcile additive supplier identity state from db/state')) {
    return
}

$phaseOne = [Text.StringBuilder]::new()
[void]$phaseOne.AppendLine('\set ON_ERROR_STOP on')
[void]$phaseOne.AppendLine('BEGIN;')
foreach ($column in $supplierColumns) {
    $profileDefinition = Get-StateColumnDefinition $profileState $column
    $snapshotDefinition = (Get-StateColumnDefinition $snapshotState $column) -replace '(?i)\s+NOT\s+NULL\s*$', ' NULL'
    [void]$phaseOne.AppendLine("ALTER TABLE pos.sales_invoice_header_profiles ADD COLUMN IF NOT EXISTS $profileDefinition;")
    [void]$phaseOne.AppendLine("ALTER TABLE pos.fiscal_document_header_snapshots ADD COLUMN IF NOT EXISTS $snapshotDefinition;")
}
[void]$phaseOne.AppendLine('COMMIT;')
[void](Invoke-Psql $phaseOne.ToString())

$incompatibilitySql = @'
SELECT concat_ws('|',
  (SELECT count(*) FROM pos.sales_invoice_header_profiles WHERE lifecycle_status='APPROVED' AND (supplier_developer_registered_name IS NULL OR supplier_developer_address IS NULL OR supplier_developer_tin IS NULL)),
  (SELECT count(*) FROM pos.fiscal_document_header_snapshots WHERE supplier_developer_registered_name IS NULL OR supplier_developer_address IS NULL OR supplier_developer_tin IS NULL)
);
'@
$incompatibilities = (Invoke-Psql $incompatibilitySql -Scalar).Split('|')
if ([int64]$incompatibilities[0] -gt 0 -or [int64]$incompatibilities[1] -gt 0) {
    throw "$($incompatibilities[0]) approved profile rows lack authoritative supplier identity; $($incompatibilities[1]) historical snapshot rows lack authoritative supplier identity. Columns were added nullable and no values were fabricated."
}

$phaseTwo = [Text.StringBuilder]::new()
[void]$phaseTwo.AppendLine('\set ON_ERROR_STOP on')
[void]$phaseTwo.AppendLine('BEGIN;')
foreach ($column in $supplierColumns) {
    [void]$phaseTwo.AppendLine("ALTER TABLE pos.fiscal_document_header_snapshots ALTER COLUMN $(Quote-Identifier $column) SET NOT NULL;")
}
foreach ($constraint in $profileConstraints) {
    $definition = Get-StateConstraintDefinition $profileState $constraint
    [void]$phaseTwo.AppendLine("ALTER TABLE pos.sales_invoice_header_profiles DROP CONSTRAINT IF EXISTS $(Quote-Identifier $constraint);")
    [void]$phaseTwo.AppendLine("ALTER TABLE pos.sales_invoice_header_profiles ADD CONSTRAINT $(Quote-Identifier $constraint) $definition;")
}
foreach ($constraint in $snapshotConstraints) {
    $definition = Get-StateConstraintDefinition $snapshotState $constraint
    [void]$phaseTwo.AppendLine("ALTER TABLE pos.fiscal_document_header_snapshots DROP CONSTRAINT IF EXISTS $(Quote-Identifier $constraint);")
    [void]$phaseTwo.AppendLine("ALTER TABLE pos.fiscal_document_header_snapshots ADD CONSTRAINT $(Quote-Identifier $constraint) $definition;")
}
[void]$phaseTwo.AppendLine('COMMIT;')
[void](Invoke-Psql $phaseTwo.ToString())

$after = Get-DatabaseFacts
if ($before.profile_rows -ne $after.profile_rows -or $before.profile_id_hash -ne $after.profile_id_hash -or
    $before.snapshot_rows -ne $after.snapshot_rows -or $before.snapshot_id_hash -ne $after.snapshot_id_hash -or
    $before.fiscal_document_rows -ne $after.fiscal_document_rows -or
    $before.electronic_journal_rows -ne $after.electronic_journal_rows) {
    throw 'State reconciliation changed protected row counts or identities.'
}
if ($after.supplier_profile_columns -ne 3 -or $after.supplier_snapshot_columns -ne 3 -or
    $after.supplier_profile_constraints -ne 4 -or $after.supplier_snapshot_constraints -ne 3) {
    throw 'Persistent database does not match the required canonical supplier state after reconciliation.'
}

$inspection.after = $after
$inspection.result = 'RECONCILED'
Write-Evidence $inspection
$inspection | ConvertTo-Json -Depth 8
