[CmdletBinding()]
param([switch] $Check)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$sourceRoot = Join-Path $repoRoot 'db\reference-data\controlled-codes\source'
$index = Get-Content -Raw -LiteralPath (Join-Path $sourceRoot 'controlled_code_source_index.json') | ConvertFrom-Json
$familyEntry = @($index.family_sources | Where-Object code_set_key -eq 'discount_privilege_type')
if ($familyEntry.Count -ne 1) { throw 'Expected one indexed discount_privilege_type family.' }
$family = Get-Content -Raw -LiteralPath (Join-Path $sourceRoot $familyEntry[0].path) | ConvertFrom-Json
if ($family.code_sets.Count -ne 1 -or $family.code_sets[0].codes.Count -ne 1) {
    throw 'This slice must contain exactly one code set and one approved code.'
}
$set = $family.code_sets[0]
$code = $set.codes[0]
if ($set.code_set_key -ne 'discount_privilege_type' -or
    $code.code_key -ne 'statutory_discount_and_vat_privilege') {
    throw 'Unexpected controlled-code source identity.'
}

function New-SourceUuid([string] $name) {
    $hex = ([guid] $index.uuid_namespace).ToString('N')
    $namespaceBytes = New-Object byte[] 16
    for ($i = 0; $i -lt 16; $i++) {
        $namespaceBytes[$i] = [Convert]::ToByte($hex.Substring($i * 2, 2), 16)
    }
    $nameBytes = [Text.Encoding]::UTF8.GetBytes($name)
    $inputBytes = New-Object byte[] (16 + $nameBytes.Length)
    [Array]::Copy($namespaceBytes, $inputBytes, 16)
    [Array]::Copy($nameBytes, 0, $inputBytes, 16, $nameBytes.Length)
    $sha1 = [Security.Cryptography.SHA1]::Create()
    try { $hash = $sha1.ComputeHash($inputBytes) }
    finally { $sha1.Dispose() }
    $hash[6] = [byte] (($hash[6] -band 0x0f) -bor 0x50)
    $hash[8] = [byte] (($hash[8] -band 0x3f) -bor 0x80)
    $uuidHex = -join ($hash[0..15] | ForEach-Object { $_.ToString('x2') })
    return ([guid]::ParseExact($uuidHex, 'N')).ToString('D')
}

function Quote-Sql([string] $value) {
    return "'" + $value.Replace("'", "''") + "'"
}

$setId = New-SourceUuid ($index.uuid_name_inputs.code_set.Replace('<code_set_key>', $set.code_set_key))
$codeId = New-SourceUuid ($index.uuid_name_inputs.code.Replace('<code_set_key>', $set.code_set_key).Replace('<code_key>', $code.code_key))
$setName = Quote-Sql $set.display_name
$setDescription = Quote-Sql $set.description
$setOwner = Quote-Sql $set.governance_owner
$setSource = Quote-Sql $set.source_ref
$codeName = Quote-Sql $code.display_name
$codeDescription = Quote-Sql $code.description
$codeSource = Quote-Sql $code.source_ref
$setEffective = Quote-Sql $set.effective_start_at
$codeEffective = Quote-Sql $code.effective_start_at

$sql = @"
-- GENERATED FILE - DO NOT EDIT MANUALLY.
-- Source: db/reference-data/controlled-codes/source/families/discount_privilege_type.json
-- Namespace: $($index.uuid_namespace)
BEGIN;

INSERT INTO pos.controlled_code_sets (
    controlled_code_set_id, code_set_key, display_name, description, governance_owner,
    source_ref, is_active, effective_start_at, created_at, updated_at)
VALUES (
    '$setId'::uuid, 'discount_privilege_type', $setName, $setDescription, $setOwner,
    $setSource, true, $setEffective::timestamptz, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (controlled_code_set_id) DO NOTHING;

INSERT INTO pos.controlled_codes (
    controlled_code_id, controlled_code_set_id, code_key, display_name, description,
    source_ref, sort_order, is_active, effective_start_at, created_at, updated_at)
VALUES (
    '$codeId'::uuid, '$setId'::uuid, 'statutory_discount_and_vat_privilege',
    $codeName, $codeDescription, $codeSource, $($code.sort_order), true,
    $codeEffective::timestamptz, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (controlled_code_id) DO NOTHING;

COMMIT;
"@
$sql = $sql.Replace("`r`n", "`n").TrimEnd() + "`n"
$outputPath = Join-Path $repoRoot 'db\reference-data\controlled-codes\generated\sql\011_controlled_codes_discount_privilege_type.sql'
if ($Check) {
    if (-not (Test-Path -LiteralPath $outputPath) -or
        [IO.File]::ReadAllText($outputPath) -ne $sql) {
        throw 'Generated discount privilege SQL does not match its indexed source.'
    }
}
else {
    [IO.File]::WriteAllText($outputPath, $sql, (New-Object Text.UTF8Encoding($false)))
}
Write-Output "discount_privilege_type=$setId statutory_discount_and_vat_privilege=$codeId check=$Check"
