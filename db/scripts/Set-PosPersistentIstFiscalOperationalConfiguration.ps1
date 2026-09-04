<#
.SYNOPSIS
Materializes stable PITX WebPay fiscal issuance configuration in an approved persistent IST database.

.DESCRIPTION
Canonical controlled-code JSON remains authoritative. This script applies only the additive generated
fiscal-issuance code slice, then creates or reconciles the PITX WebPay terminal and Sales Invoice
sequence policy. Existing sequence counters are never updated or reset.
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
. (Join-Path $PSScriptRoot 'PosPersistentIstTooling.ps1')
Assert-PosPersistentIstTarget $ContainerName $DatabaseName $DatabaseUser

$siteId = '2d1dcdf8-f563-537c-8542-0bde7cc9da97'
$sitePosServerId = '3a138565-1b88-55f8-c83d-5380db6edccc'
$fiscalIdentityId = 'ad02beb5-b8cd-4545-9ff2-5586782c686a'
$headerProfileId = 'cb90e882-89f3-4bf3-975c-a6fb10dd765c'
$terminalId = 'b71f74f7-8e56-5fda-b257-5d9823ab8520'
$policyId = '2e4252dd-38f0-5424-bee2-0c864b41f6b2'
$stateId = 'a46c9d75-5b50-5cb6-927e-d5c0b44a4edc'

function Get-PitxFiscalOperationalFacts {
    $sql = @"
SELECT json_build_object(
  'site_pos_server_count', (SELECT count(*) FROM pos.site_pos_servers WHERE site_pos_server_id='$sitePosServerId' AND central_pms_site_ref='$siteId' AND is_active),
  'fiscal_identity_relationship_count', (SELECT count(*) FROM pos.site_pos_server_fiscal_identity_history WHERE site_pos_server_id='$sitePosServerId' AND fiscal_identity_id='$fiscalIdentityId' AND effective_start_at<=CURRENT_TIMESTAMP AND (effective_end_at IS NULL OR effective_end_at>CURRENT_TIMESTAMP)),
  'approved_profile_count', (SELECT count(*) FROM pos.sales_invoice_header_profiles WHERE sales_invoice_header_profile_id='$headerProfileId' AND site_id='$siteId' AND site_pos_server_id='$sitePosServerId' AND fiscal_identity_id='$fiscalIdentityId' AND lifecycle_status='APPROVED' AND effective_from<=CURRENT_TIMESTAMP AND effective_to IS NULL),
  'webpay_terminal_count', (SELECT count(*) FROM pos.channel_terminals WHERE site_pos_server_id='$sitePosServerId' AND channel_terminal_code='PITX-L3-WEBPAY-01' AND is_active),
  'sales_invoice_policy_count', (SELECT count(*) FROM pos.fiscal_sequence_policies WHERE site_pos_server_id='$sitePosServerId' AND policy_code='PITX-L3-SI' AND effective_start_at<=CURRENT_TIMESTAMP AND (effective_end_at IS NULL OR effective_end_at>CURRENT_TIMESTAMP)),
  'sequence_state_count', (SELECT count(*) FROM pos.fiscal_sequence_states WHERE fiscal_sequence_policy_id='$policyId'),
  'sequence_current_value', (SELECT current_sequence_value FROM pos.fiscal_sequence_states WHERE fiscal_sequence_policy_id='$policyId'),
  'sequence_last_reserved_value', (SELECT last_reserved_sequence_value FROM pos.fiscal_sequence_states WHERE fiscal_sequence_policy_id='$policyId'),
  'sequence_last_issued_value', (SELECT last_issued_sequence_value FROM pos.fiscal_sequence_states WHERE fiscal_sequence_policy_id='$policyId'),
  'fiscal_document_count', (SELECT count(*) FROM pos.fiscal_documents),
  'electronic_journal_count', (SELECT count(*) FROM pos.electronic_journal_records),
  'controlled_code_set_count', (SELECT count(*) FROM pos.controlled_code_sets),
  'controlled_code_count', (SELECT count(*) FROM pos.controlled_codes)
)::text;
"@
    return (Invoke-PosPersistentIstPsql $sql $ContainerName $DatabaseName $DatabaseUser -Scalar | ConvertFrom-Json)
}

$before = Get-PitxFiscalOperationalFacts
$result = [ordered]@{
    mode = $Mode
    database = $DatabaseName
    site_id = $siteId
    site_pos_server_id = $sitePosServerId
    fiscal_identity_id = $fiscalIdentityId
    header_profile_id = $headerProfileId
    channel_terminal_id = $terminalId
    fiscal_sequence_policy_id = $policyId
    fiscal_sequence_state_id = $stateId
    before = $before
    status = 'INSPECTED'
}
if ($Mode -eq 'Inspect') {
    Write-PosPersistentIstEvidence $result $EvidenceDir 'pos-persistent-ist-fiscal-operational-configuration.json'
    $result | ConvertTo-Json -Depth 10
    return
}

if (-not $PSCmdlet.ShouldProcess("$ContainerName/$DatabaseName", 'Apply additive canonical fiscal codes and stable PITX fiscal operational configuration')) {
    return
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$codeSqlPath = Join-Path $repoRoot 'db\reference-data\controlled-codes\generated\sql\010_controlled_codes_fiscal_issuance.sql'
[void](Invoke-PosPersistentIstPsql ([IO.File]::ReadAllText($codeSqlPath)) $ContainerName $DatabaseName $DatabaseUser)

$applySql = @"
BEGIN;

DO `$`$
DECLARE
  pair text;
  pair_count integer;
BEGIN
  IF (SELECT count(*) FROM pos.site_pos_servers WHERE site_pos_server_id='$sitePosServerId' AND central_pms_site_ref='$siteId' AND is_active) <> 1 THEN
    RAISE EXCEPTION 'PITX Site POS Server identity is missing, inactive, or conflicting';
  END IF;
  IF (SELECT count(*) FROM pos.site_pos_server_fiscal_identity_history WHERE site_pos_server_id='$sitePosServerId' AND fiscal_identity_id='$fiscalIdentityId' AND effective_start_at<=CURRENT_TIMESTAMP AND (effective_end_at IS NULL OR effective_end_at>CURRENT_TIMESTAMP)) <> 1 THEN
    RAISE EXCEPTION 'PITX effective fiscal identity relationship must resolve exactly once';
  END IF;
  IF (SELECT count(*) FROM pos.sales_invoice_header_profiles WHERE sales_invoice_header_profile_id='$headerProfileId' AND site_id='$siteId' AND site_pos_server_id='$sitePosServerId' AND fiscal_identity_id='$fiscalIdentityId' AND lifecycle_status='APPROVED' AND effective_from<=CURRENT_TIMESTAMP AND effective_to IS NULL) <> 1 THEN
    RAISE EXCEPTION 'PITX current approved Sales Invoice header profile must resolve exactly once';
  END IF;
  FOREACH pair IN ARRAY ARRAY[
    'channel_terminal_type:webpay',
    'pos_terminal_operational_status:active',
    'channel_terminal_health_status:online',
    'fiscal_document_type:sales_invoice',
    'fiscal_sequence_family:sales_invoice',
    'fiscal_sequence_policy_status:active',
    'fiscal_sequence_state:active'
  ] LOOP
    SELECT count(*) INTO pair_count
    FROM pos.controlled_codes c
    JOIN pos.controlled_code_sets s ON s.controlled_code_set_id=c.controlled_code_set_id
    WHERE s.code_set_key=split_part(pair,':',1)
      AND c.code_key=split_part(pair,':',2)
      AND s.is_active AND c.is_active
      AND (s.effective_start_at IS NULL OR s.effective_start_at<=CURRENT_TIMESTAMP)
      AND (s.effective_end_at IS NULL OR s.effective_end_at>CURRENT_TIMESTAMP)
      AND (c.effective_start_at IS NULL OR c.effective_start_at<=CURRENT_TIMESTAMP)
      AND (c.effective_end_at IS NULL OR c.effective_end_at>CURRENT_TIMESTAMP);
    IF pair_count <> 1 THEN RAISE EXCEPTION 'Controlled code % must resolve exactly once', pair; END IF;
  END LOOP;
  IF EXISTS (SELECT 1 FROM pos.channel_terminals WHERE site_pos_server_id='$sitePosServerId' AND channel_terminal_code='PITX-L3-WEBPAY-01' AND channel_terminal_id<>'$terminalId') THEN
    RAISE EXCEPTION 'PITX WebPay terminal natural key has a conflicting identity';
  END IF;
  IF EXISTS (SELECT 1 FROM pos.fiscal_sequence_policies WHERE site_pos_server_id='$sitePosServerId' AND policy_code='PITX-L3-SI' AND fiscal_sequence_policy_id<>'$policyId') THEN
    RAISE EXCEPTION 'PITX Sales Invoice sequence policy natural key has a conflicting identity';
  END IF;
END
`$`$;

UPDATE pos.site_pos_servers
SET reporting_timezone_name=COALESCE(reporting_timezone_name,'Asia/Manila'),
    business_day_cutoff_local_time=COALESCE(business_day_cutoff_local_time,'00:00:00'),
    updated_at=CURRENT_TIMESTAMP
WHERE site_pos_server_id='$sitePosServerId'
  AND (reporting_timezone_name IS NULL OR business_day_cutoff_local_time IS NULL);

INSERT INTO pos.channel_terminals(
  channel_terminal_id,site_pos_server_id,channel_terminal_code,display_name,
  channel_terminal_type_code_id,is_logical_channel,is_physical_terminal,
  fiscal_identity_id,health_status_code_id,operational_status_code_id,is_active)
SELECT '$terminalId','$sitePosServerId','PITX-L3-WEBPAY-01','PITX Level 3 WebPay',
  type_code.controlled_code_id,true,false,'$fiscalIdentityId',
  health_code.controlled_code_id,status_code.controlled_code_id,true
FROM pos.controlled_codes type_code
JOIN pos.controlled_code_sets type_set ON type_set.controlled_code_set_id=type_code.controlled_code_set_id AND type_set.code_set_key='channel_terminal_type'
JOIN pos.controlled_codes health_code ON health_code.code_key='online'
JOIN pos.controlled_code_sets health_set ON health_set.controlled_code_set_id=health_code.controlled_code_set_id AND health_set.code_set_key='channel_terminal_health_status'
JOIN pos.controlled_codes status_code ON status_code.code_key='active'
JOIN pos.controlled_code_sets status_set ON status_set.controlled_code_set_id=status_code.controlled_code_set_id AND status_set.code_set_key='pos_terminal_operational_status'
WHERE type_code.code_key='webpay'
ON CONFLICT(channel_terminal_id) DO UPDATE SET
  display_name=EXCLUDED.display_name,
  channel_terminal_type_code_id=EXCLUDED.channel_terminal_type_code_id,
  is_logical_channel=EXCLUDED.is_logical_channel,
  is_physical_terminal=EXCLUDED.is_physical_terminal,
  fiscal_identity_id=EXCLUDED.fiscal_identity_id,
  health_status_code_id=EXCLUDED.health_status_code_id,
  operational_status_code_id=EXCLUDED.operational_status_code_id,
  is_active=EXCLUDED.is_active,
  updated_at=CURRENT_TIMESTAMP
WHERE (channel_terminals.display_name,channel_terminals.channel_terminal_type_code_id,
       channel_terminals.is_logical_channel,channel_terminals.is_physical_terminal,
       channel_terminals.fiscal_identity_id,channel_terminals.health_status_code_id,
       channel_terminals.operational_status_code_id,channel_terminals.is_active)
  IS DISTINCT FROM
      (EXCLUDED.display_name,EXCLUDED.channel_terminal_type_code_id,
       EXCLUDED.is_logical_channel,EXCLUDED.is_physical_terminal,
       EXCLUDED.fiscal_identity_id,EXCLUDED.health_status_code_id,
       EXCLUDED.operational_status_code_id,EXCLUDED.is_active);

INSERT INTO pos.fiscal_sequence_policies(
  fiscal_sequence_policy_id,site_pos_server_id,sequence_family_code_id,
  document_type_code_id,policy_code,display_name,description,prefix_text,
  suffix_text,padding_length,current_policy_status_code_id,effective_start_at,policy_context)
SELECT '$policyId','$sitePosServerId',family.controlled_code_id,document_type.controlled_code_id,
  'PITX-L3-SI','PITX Level 3 Sales Invoice',
  'Persistent non-production IST Sales Invoice sequence policy.','SI-',NULL,8,
  policy_status.controlled_code_id,'2026-09-04T00:00:00Z','{"environment":"IST","site_code":"PITX-LEVEL-3"}'::jsonb
FROM pos.controlled_codes family
JOIN pos.controlled_code_sets family_set ON family_set.controlled_code_set_id=family.controlled_code_set_id AND family_set.code_set_key='fiscal_sequence_family'
JOIN pos.controlled_codes document_type ON document_type.code_key='sales_invoice'
JOIN pos.controlled_code_sets document_set ON document_set.controlled_code_set_id=document_type.controlled_code_set_id AND document_set.code_set_key='fiscal_document_type'
JOIN pos.controlled_codes policy_status ON policy_status.code_key='active'
JOIN pos.controlled_code_sets policy_set ON policy_set.controlled_code_set_id=policy_status.controlled_code_set_id AND policy_set.code_set_key='fiscal_sequence_policy_status'
WHERE family.code_key='sales_invoice'
ON CONFLICT(fiscal_sequence_policy_id) DO UPDATE SET
  sequence_family_code_id=EXCLUDED.sequence_family_code_id,
  document_type_code_id=EXCLUDED.document_type_code_id,
  display_name=EXCLUDED.display_name,
  description=EXCLUDED.description,
  prefix_text=EXCLUDED.prefix_text,
  suffix_text=EXCLUDED.suffix_text,
  padding_length=EXCLUDED.padding_length,
  current_policy_status_code_id=EXCLUDED.current_policy_status_code_id,
  effective_start_at=EXCLUDED.effective_start_at,
  effective_end_at=NULL,
  policy_context=EXCLUDED.policy_context,
  updated_at=CURRENT_TIMESTAMP
WHERE (fiscal_sequence_policies.sequence_family_code_id,
       fiscal_sequence_policies.document_type_code_id,
       fiscal_sequence_policies.display_name,
       fiscal_sequence_policies.description,
       fiscal_sequence_policies.prefix_text,
       fiscal_sequence_policies.suffix_text,
       fiscal_sequence_policies.padding_length,
       fiscal_sequence_policies.current_policy_status_code_id,
       fiscal_sequence_policies.effective_start_at,
       fiscal_sequence_policies.effective_end_at,
       fiscal_sequence_policies.policy_context)
  IS DISTINCT FROM
      (EXCLUDED.sequence_family_code_id,EXCLUDED.document_type_code_id,
       EXCLUDED.display_name,EXCLUDED.description,EXCLUDED.prefix_text,
       EXCLUDED.suffix_text,EXCLUDED.padding_length,
       EXCLUDED.current_policy_status_code_id,EXCLUDED.effective_start_at,
       NULL,EXCLUDED.policy_context);

INSERT INTO pos.fiscal_sequence_states(
  fiscal_sequence_state_id,fiscal_sequence_policy_id,current_sequence_value,
  last_reserved_sequence_value,last_issued_sequence_value,sequence_state_code_id,
  last_transition_at,state_context)
SELECT '$stateId','$policyId',0,NULL,NULL,state_code.controlled_code_id,NULL,
       '{"environment":"IST","site_code":"PITX-LEVEL-3"}'::jsonb
FROM pos.controlled_codes state_code
JOIN pos.controlled_code_sets state_set ON state_set.controlled_code_set_id=state_code.controlled_code_set_id
WHERE state_set.code_set_key='fiscal_sequence_state' AND state_code.code_key='active'
ON CONFLICT(fiscal_sequence_policy_id) DO NOTHING;

COMMIT;
"@
[void](Invoke-PosPersistentIstPsql $applySql $ContainerName $DatabaseName $DatabaseUser)
$after = Get-PitxFiscalOperationalFacts
if ($before.fiscal_document_count -ne $after.fiscal_document_count -or
    $before.electronic_journal_count -ne $after.electronic_journal_count) {
    throw 'Operational configuration changed fiscal business transaction counts.'
}
if ($after.site_pos_server_count -ne 1 -or $after.fiscal_identity_relationship_count -ne 1 -or
    $after.approved_profile_count -ne 1 -or $after.webpay_terminal_count -ne 1 -or
    $after.sales_invoice_policy_count -ne 1 -or $after.sequence_state_count -ne 1) {
    throw 'PITX static fiscal operational configuration did not resolve exactly once.'
}
if ($before.sequence_state_count -eq 1 -and
    ($before.sequence_current_value -ne $after.sequence_current_value -or
     $before.sequence_last_reserved_value -ne $after.sequence_last_reserved_value -or
     $before.sequence_last_issued_value -ne $after.sequence_last_issued_value)) {
    throw 'Operational configuration changed existing fiscal sequence history.'
}
$result.after = $after
$result.status = 'APPLIED'
Write-PosPersistentIstEvidence $result $EvidenceDir 'pos-persistent-ist-fiscal-operational-configuration.json'
$result | ConvertTo-Json -Depth 10
