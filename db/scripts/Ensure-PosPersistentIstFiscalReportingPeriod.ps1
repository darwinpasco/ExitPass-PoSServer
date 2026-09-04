<#
.SYNOPSIS
Ensures the current PITX business-day reporting period exists in a non-production persistent IST database.

.DESCRIPTION
Period lifecycle is separate from static configuration. This command derives one half-open
Asia/Manila business day, reuses a current OPEN period, rejects overlap or ambiguity, and
never mutates CLOSED periods.
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Inspect', 'Apply')]
    [string] $Mode = 'Inspect',
    [string] $ContainerName = 'exitpass-pos-ist-persistent-db',
    [string] $DatabaseName = 'exitpass_pos_ist',
    [string] $DatabaseUser = 'exitpass_ist',
    [DateTimeOffset] $At = [DateTimeOffset]::UtcNow,
    [string] $EvidenceDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'PosPersistentIstTooling.ps1')
Assert-PosPersistentIstTarget $ContainerName $DatabaseName $DatabaseUser

$sitePosServerId = '3a138565-1b88-55f8-c83d-5380db6edccc'
$fiscalIdentityId = 'ad02beb5-b8cd-4545-9ff2-5586782c686a'
$contractId = 'f6766f48-62f0-513f-b9eb-e61c2f3e8c66'
$namespace = [Guid]'128592fe-b62e-5c3e-a1dc-237cfe3fc525'
try {
    $timezone = [TimeZoneInfo]::FindSystemTimeZoneById('Asia/Manila')
}
catch [TimeZoneNotFoundException] {
    $timezone = [TimeZoneInfo]::FindSystemTimeZoneById('Singapore Standard Time')
}
$localAt = [TimeZoneInfo]::ConvertTime($At, $timezone)
$businessDateText = $localAt.Date.ToString('yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture)
$startLocal = [DateTime]::SpecifyKind($localAt.Date, [DateTimeKind]::Unspecified)
$endLocal = $startLocal.AddDays(1)
$startAt = [TimeZoneInfo]::ConvertTimeToUtc($startLocal, $timezone)
$endAt = [TimeZoneInfo]::ConvertTimeToUtc($endLocal, $timezone)
$periodName = 'pos.fiscal_reporting_periods:' + $sitePosServerId + ':' + $fiscalIdentityId + ':PHP:' + $businessDateText
$periodId = New-PosPersistentIstUuidV5 $namespace $periodName
$atUtc = $At.ToUniversalTime().ToString('O')
$startUtc = $startAt.ToString('O')
$endUtc = $endAt.ToString('O')

$factsSql = @"
SELECT json_build_object(
  'current_open_period_count', (
    SELECT count(*) FROM pos.fiscal_reporting_periods
    WHERE site_pos_server_id='$sitePosServerId'
      AND fiscal_identity_id='$fiscalIdentityId'
      AND currency_code='PHP'
      AND period_status_code_id='1a6f7021-bc84-5c01-afaa-c5d6685633c8'
      AND '$atUtc'::timestamptz>=period_start_at
      AND '$atUtc'::timestamptz<period_end_at),
  'current_open_period_id', (
    SELECT min(fiscal_reporting_period_id::text) FROM pos.fiscal_reporting_periods
    WHERE site_pos_server_id='$sitePosServerId'
      AND fiscal_identity_id='$fiscalIdentityId'
      AND currency_code='PHP'
      AND period_status_code_id='1a6f7021-bc84-5c01-afaa-c5d6685633c8'
      AND '$atUtc'::timestamptz>=period_start_at
      AND '$atUtc'::timestamptz<period_end_at),
  'overlap_count', (
    SELECT count(*) FROM pos.fiscal_reporting_periods
    WHERE site_pos_server_id='$sitePosServerId'
      AND fiscal_identity_id='$fiscalIdentityId'
      AND currency_code='PHP'
      AND tstzrange(period_start_at,period_end_at,'[)') && tstzrange('$startUtc','$endUtc','[)')),
  'closed_period_count', (
    SELECT count(*) FROM pos.fiscal_reporting_periods
    WHERE site_pos_server_id='$sitePosServerId'
      AND fiscal_identity_id='$fiscalIdentityId'
      AND currency_code='PHP'
      AND period_status_code_id='af7ee931-a023-507e-81a4-17adf047eb94'),
  'fiscal_document_count', (SELECT count(*) FROM pos.fiscal_documents),
  'electronic_journal_count', (SELECT count(*) FROM pos.electronic_journal_records)
)::text;
"@
function Get-PeriodFacts {
    return (Invoke-PosPersistentIstPsql $factsSql $ContainerName $DatabaseName $DatabaseUser -Scalar | ConvertFrom-Json)
}

$before = Get-PeriodFacts
$result = [ordered]@{
    mode = $Mode
    database = $DatabaseName
    site_pos_server_id = $sitePosServerId
    fiscal_identity_id = $fiscalIdentityId
    reporting_contract_version_id = $contractId
    fiscal_reporting_period_id = $periodId.ToString()
    business_day_date = $businessDateText
    period_start_at = $startUtc
    period_end_at = $endUtc
    reporting_timezone_name = 'Asia/Manila'
    business_day_cutoff_local_time = '00:00:00'
    before = $before
    status = 'INSPECTED'
}
if ($Mode -eq 'Inspect') {
    Write-PosPersistentIstEvidence $result $EvidenceDir 'pos-persistent-ist-fiscal-reporting-period.json'
    $result | ConvertTo-Json -Depth 10
    return
}

if (-not $PSCmdlet.ShouldProcess("$ContainerName/$DatabaseName", "Ensure PITX OPEN fiscal reporting period $businessDateText")) {
    return
}

$applySql = @"
BEGIN;
SELECT pg_advisory_xact_lock(hashtextextended('persistent-ist-period:$($sitePosServerId):$($fiscalIdentityId):PHP',0));

DO `$`$
BEGIN
  IF (SELECT count(*) FROM pos.fiscal_reporting_contract_versions
      WHERE fiscal_reporting_contract_version_id='$contractId'
        AND contract_key='pos-server-fiscal-reporting'
        AND contract_version='v1'
        AND is_active
        AND effective_from<='$atUtc'::timestamptz
        AND (effective_to IS NULL OR effective_to>'$atUtc'::timestamptz)) <> 1 THEN
    RAISE EXCEPTION 'Canonical fiscal reporting contract version must resolve exactly once';
  END IF;
  IF (SELECT count(*) FROM pos.fiscal_reporting_periods
      WHERE site_pos_server_id='$sitePosServerId'
        AND fiscal_identity_id='$fiscalIdentityId'
        AND currency_code='PHP'
        AND period_status_code_id='1a6f7021-bc84-5c01-afaa-c5d6685633c8'
        AND '$atUtc'::timestamptz>=period_start_at
        AND '$atUtc'::timestamptz<period_end_at) > 1 THEN
    RAISE EXCEPTION 'Multiple current OPEN PITX fiscal reporting periods are not allowed';
  END IF;
END
`$`$;

INSERT INTO pos.fiscal_reporting_periods(
  fiscal_reporting_period_id,fiscal_reporting_contract_version_id,
  site_pos_server_id,fiscal_identity_id,period_status_code_id,business_day_date,
  period_start_at,period_end_at,reporting_timezone_name,
  business_day_cutoff_local_time,currency_code,period_sequence,
  expected_prior_period_id,opened_at,created_by_ref,updated_by_ref)
SELECT '$periodId','$contractId','$sitePosServerId','$fiscalIdentityId',
  '1a6f7021-bc84-5c01-afaa-c5d6685633c8','$businessDateText',
  '$startUtc','$endUtc','Asia/Manila','00:00:00','PHP',
  COALESCE((SELECT max(period_sequence)+1 FROM pos.fiscal_reporting_periods WHERE site_pos_server_id='$sitePosServerId'),1),
  (SELECT fiscal_reporting_period_id FROM pos.fiscal_reporting_periods
   WHERE site_pos_server_id='$sitePosServerId'
     AND fiscal_identity_id='$fiscalIdentityId'
     AND currency_code='PHP'
     AND period_end_at<='$startUtc'::timestamptz
   ORDER BY period_end_at DESC,period_sequence DESC LIMIT 1),
  '$atUtc','persistent-ist:restart-41','persistent-ist:restart-41'
WHERE NOT EXISTS (
  SELECT 1 FROM pos.fiscal_reporting_periods
  WHERE site_pos_server_id='$sitePosServerId'
    AND fiscal_identity_id='$fiscalIdentityId'
    AND currency_code='PHP'
    AND period_status_code_id='1a6f7021-bc84-5c01-afaa-c5d6685633c8'
    AND '$atUtc'::timestamptz>=period_start_at
    AND '$atUtc'::timestamptz<period_end_at)
AND NOT EXISTS (
  SELECT 1 FROM pos.fiscal_reporting_periods
  WHERE site_pos_server_id='$sitePosServerId'
    AND fiscal_identity_id='$fiscalIdentityId'
    AND currency_code='PHP'
    AND tstzrange(period_start_at,period_end_at,'[)') && tstzrange('$startUtc','$endUtc','[)'));

DO `$`$
BEGIN
  IF (SELECT count(*) FROM pos.fiscal_reporting_periods
      WHERE site_pos_server_id='$sitePosServerId'
        AND fiscal_identity_id='$fiscalIdentityId'
        AND currency_code='PHP'
        AND period_status_code_id='1a6f7021-bc84-5c01-afaa-c5d6685633c8'
        AND '$atUtc'::timestamptz>=period_start_at
        AND '$atUtc'::timestamptz<period_end_at) <> 1 THEN
    RAISE EXCEPTION 'Exactly one current OPEN PITX fiscal reporting period is required; overlap or missing period detected';
  END IF;
END
`$`$;
COMMIT;
"@
[void](Invoke-PosPersistentIstPsql $applySql $ContainerName $DatabaseName $DatabaseUser)
$after = Get-PeriodFacts
if ($after.current_open_period_count -ne 1) { throw 'Current PITX OPEN reporting period did not resolve exactly once.' }
if ($before.fiscal_document_count -ne $after.fiscal_document_count -or
    $before.electronic_journal_count -ne $after.electronic_journal_count -or
    $before.closed_period_count -ne $after.closed_period_count) {
    throw 'Reporting-period ensure changed protected fiscal history.'
}
$result.after = $after
$result.status = if ($before.current_open_period_count -eq 1) { 'REUSED' } else { 'CREATED' }
Write-PosPersistentIstEvidence $result $EvidenceDir 'pos-persistent-ist-fiscal-reporting-period.json'
$result | ConvertTo-Json -Depth 10
