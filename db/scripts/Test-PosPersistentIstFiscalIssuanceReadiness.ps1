<#
.SYNOPSIS
Evaluates fail-closed PITX WebPay Sales Invoice issuance readiness.
#>

[CmdletBinding()]
param(
    [string] $ContainerName = 'exitpass-pos-ist-persistent-db',
    [string] $DatabaseName = 'exitpass_pos_ist',
    [string] $DatabaseUser = 'exitpass_ist',
    [DateTimeOffset] $At = [DateTimeOffset]::UtcNow,
    [switch] $RequireReady,
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
$atUtc = $At.ToUniversalTime().ToString('O')

$sql = @"
WITH counts AS (
  SELECT
    (SELECT count(*) FROM pos.site_pos_servers
     WHERE site_pos_server_id='$sitePosServerId' AND central_pms_site_ref='$siteId' AND is_active) AS site_pos_server,
    (SELECT count(*) FROM pos.site_pos_server_fiscal_identity_history history
     JOIN pos.fiscal_identities identity ON identity.fiscal_identity_id=history.fiscal_identity_id
     WHERE history.site_pos_server_id='$sitePosServerId' AND history.fiscal_identity_id='$fiscalIdentityId'
       AND identity.fiscal_identity_status='APPROVED' AND identity.is_active
       AND history.effective_start_at<='$atUtc'::timestamptz
       AND (history.effective_end_at IS NULL OR history.effective_end_at>'$atUtc'::timestamptz)) AS fiscal_identity,
    (SELECT count(*) FROM pos.sales_invoice_header_profiles
     WHERE sales_invoice_header_profile_id='$headerProfileId' AND site_id='$siteId'
       AND site_pos_server_id='$sitePosServerId' AND fiscal_identity_id='$fiscalIdentityId'
       AND lifecycle_status='APPROVED' AND effective_from<='$atUtc'::timestamptz
       AND effective_to IS NULL) AS approved_profile,
    (SELECT count(*) FROM pos.channel_terminals terminal
     JOIN pos.controlled_codes type_code ON type_code.controlled_code_id=terminal.channel_terminal_type_code_id
     JOIN pos.controlled_code_sets type_set ON type_set.controlled_code_set_id=type_code.controlled_code_set_id
     JOIN pos.controlled_codes status_code ON status_code.controlled_code_id=terminal.operational_status_code_id
     JOIN pos.controlled_code_sets status_set ON status_set.controlled_code_set_id=status_code.controlled_code_set_id
     WHERE terminal.site_pos_server_id='$sitePosServerId'
       AND terminal.fiscal_identity_id='$fiscalIdentityId'
       AND terminal.channel_terminal_code='PITX-L3-WEBPAY-01'
       AND terminal.is_logical_channel AND NOT terminal.is_physical_terminal AND terminal.is_active
       AND type_set.code_set_key='channel_terminal_type' AND type_code.code_key='webpay'
       AND status_set.code_set_key='pos_terminal_operational_status' AND status_code.code_key='active'
       AND type_set.is_active AND status_set.is_active AND type_code.is_active AND status_code.is_active
       AND (type_set.effective_start_at IS NULL OR type_set.effective_start_at<='$atUtc'::timestamptz)
       AND (type_set.effective_end_at IS NULL OR type_set.effective_end_at>'$atUtc'::timestamptz)
       AND (status_set.effective_start_at IS NULL OR status_set.effective_start_at<='$atUtc'::timestamptz)
       AND (status_set.effective_end_at IS NULL OR status_set.effective_end_at>'$atUtc'::timestamptz)
       AND (type_code.effective_start_at IS NULL OR type_code.effective_start_at<='$atUtc'::timestamptz)
       AND (type_code.effective_end_at IS NULL OR type_code.effective_end_at>'$atUtc'::timestamptz)
       AND (status_code.effective_start_at IS NULL OR status_code.effective_start_at<='$atUtc'::timestamptz)
       AND (status_code.effective_end_at IS NULL OR status_code.effective_end_at>'$atUtc'::timestamptz)) AS webpay_terminal,
    (SELECT count(*) FROM pos.fiscal_sequence_policies policy
     JOIN pos.controlled_codes family ON family.controlled_code_id=policy.sequence_family_code_id
     JOIN pos.controlled_code_sets family_set ON family_set.controlled_code_set_id=family.controlled_code_set_id
     JOIN pos.controlled_codes document_type ON document_type.controlled_code_id=policy.document_type_code_id
     JOIN pos.controlled_code_sets document_set ON document_set.controlled_code_set_id=document_type.controlled_code_set_id
     JOIN pos.controlled_codes status_code ON status_code.controlled_code_id=policy.current_policy_status_code_id
     JOIN pos.controlled_code_sets status_set ON status_set.controlled_code_set_id=status_code.controlled_code_set_id
     WHERE policy.site_pos_server_id='$sitePosServerId'
       AND family_set.code_set_key='fiscal_sequence_family' AND family.code_key='sales_invoice'
       AND document_set.code_set_key='fiscal_document_type' AND document_type.code_key='sales_invoice'
       AND status_set.code_set_key='fiscal_sequence_policy_status' AND status_code.code_key='active'
       AND family_set.is_active AND document_set.is_active AND status_set.is_active
       AND family.is_active AND document_type.is_active AND status_code.is_active
       AND (family.effective_start_at IS NULL OR family.effective_start_at<='$atUtc'::timestamptz)
       AND (family.effective_end_at IS NULL OR family.effective_end_at>'$atUtc'::timestamptz)
       AND (document_type.effective_start_at IS NULL OR document_type.effective_start_at<='$atUtc'::timestamptz)
       AND (document_type.effective_end_at IS NULL OR document_type.effective_end_at>'$atUtc'::timestamptz)
       AND (status_code.effective_start_at IS NULL OR status_code.effective_start_at<='$atUtc'::timestamptz)
       AND (status_code.effective_end_at IS NULL OR status_code.effective_end_at>'$atUtc'::timestamptz)
       AND policy.effective_start_at<='$atUtc'::timestamptz
       AND (policy.effective_end_at IS NULL OR policy.effective_end_at>'$atUtc'::timestamptz)) AS sequence_policy,
    (SELECT count(*) FROM pos.fiscal_sequence_states state
     JOIN pos.fiscal_sequence_policies policy ON policy.fiscal_sequence_policy_id=state.fiscal_sequence_policy_id
     JOIN pos.controlled_codes family ON family.controlled_code_id=policy.sequence_family_code_id
     JOIN pos.controlled_code_sets family_set ON family_set.controlled_code_set_id=family.controlled_code_set_id
     JOIN pos.controlled_codes policy_status ON policy_status.controlled_code_id=policy.current_policy_status_code_id
     JOIN pos.controlled_code_sets policy_status_set ON policy_status_set.controlled_code_set_id=policy_status.controlled_code_set_id
     JOIN pos.controlled_codes state_code ON state_code.controlled_code_id=state.sequence_state_code_id
     JOIN pos.controlled_code_sets state_set ON state_set.controlled_code_set_id=state_code.controlled_code_set_id
     JOIN pos.controlled_codes document_type ON document_type.controlled_code_id=policy.document_type_code_id
     JOIN pos.controlled_code_sets document_set ON document_set.controlled_code_set_id=document_type.controlled_code_set_id
     WHERE policy.site_pos_server_id='$sitePosServerId'
       AND family_set.code_set_key='fiscal_sequence_family' AND family.code_key='sales_invoice'
       AND policy_status_set.code_set_key='fiscal_sequence_policy_status' AND policy_status.code_key='active'
       AND document_set.code_set_key='fiscal_document_type' AND document_type.code_key='sales_invoice'
       AND state_set.code_set_key='fiscal_sequence_state' AND state_code.code_key='active'
       AND family_set.is_active AND policy_status_set.is_active
       AND state_set.is_active AND document_set.is_active
       AND family.is_active AND policy_status.is_active
       AND state_code.is_active AND document_type.is_active
       AND (family.effective_start_at IS NULL OR family.effective_start_at<='$atUtc'::timestamptz)
       AND (family.effective_end_at IS NULL OR family.effective_end_at>'$atUtc'::timestamptz)
       AND (policy_status.effective_start_at IS NULL OR policy_status.effective_start_at<='$atUtc'::timestamptz)
       AND (policy_status.effective_end_at IS NULL OR policy_status.effective_end_at>'$atUtc'::timestamptz)
       AND (document_type.effective_start_at IS NULL OR document_type.effective_start_at<='$atUtc'::timestamptz)
       AND (document_type.effective_end_at IS NULL OR document_type.effective_end_at>'$atUtc'::timestamptz)
       AND policy.effective_start_at<='$atUtc'::timestamptz
       AND (policy.effective_end_at IS NULL OR policy.effective_end_at>'$atUtc'::timestamptz)
       AND (state_code.effective_start_at IS NULL OR state_code.effective_start_at<='$atUtc'::timestamptz)
       AND (state_code.effective_end_at IS NULL OR state_code.effective_end_at>'$atUtc'::timestamptz)) AS sequence_state,
    (SELECT count(*) FROM pos.fiscal_reporting_contract_versions
     WHERE fiscal_reporting_contract_version_id='f6766f48-62f0-513f-b9eb-e61c2f3e8c66'
       AND contract_key='pos-server-fiscal-reporting' AND contract_version='v1'
       AND is_active AND effective_from<='$atUtc'::timestamptz
       AND (effective_to IS NULL OR effective_to>'$atUtc'::timestamptz)) AS reporting_contract,
    (SELECT count(*) FROM pos.fiscal_reporting_periods period
     JOIN pos.controlled_codes status_code ON status_code.controlled_code_id=period.period_status_code_id
     JOIN pos.controlled_code_sets status_set ON status_set.controlled_code_set_id=status_code.controlled_code_set_id
     WHERE period.site_pos_server_id='$sitePosServerId' AND period.fiscal_identity_id='$fiscalIdentityId'
       AND period.currency_code='PHP'
       AND status_set.code_set_key='fiscal_reporting_period_status' AND status_code.code_key='open'
       AND status_set.is_active AND status_code.is_active
       AND (status_set.effective_start_at IS NULL OR status_set.effective_start_at<='$atUtc'::timestamptz)
       AND (status_set.effective_end_at IS NULL OR status_set.effective_end_at>'$atUtc'::timestamptz)
       AND (status_code.effective_start_at IS NULL OR status_code.effective_start_at<='$atUtc'::timestamptz)
       AND (status_code.effective_end_at IS NULL OR status_code.effective_end_at>'$atUtc'::timestamptz)
       AND '$atUtc'::timestamptz>=period_start_at AND '$atUtc'::timestamptz<period_end_at) AS open_period
)
SELECT json_build_object(
  'observed_at','$atUtc',
  'site_pos_server_count',site_pos_server,
  'fiscal_identity_count',fiscal_identity,
  'approved_profile_count',approved_profile,
  'webpay_terminal_count',webpay_terminal,
  'sales_invoice_sequence_policy_count',sequence_policy,
  'sequence_state_count',sequence_state,
  'reporting_contract_version_count',reporting_contract,
  'current_open_reporting_period_count',open_period,
  'ready',(site_pos_server=1 AND fiscal_identity=1 AND approved_profile=1
           AND webpay_terminal=1 AND sequence_policy=1 AND sequence_state=1
           AND reporting_contract=1 AND open_period=1)
)::text
FROM counts;
"@
$result = Invoke-PosPersistentIstPsql $sql $ContainerName $DatabaseName $DatabaseUser -Scalar | ConvertFrom-Json
Write-PosPersistentIstEvidence $result $EvidenceDir 'pos-persistent-ist-fiscal-issuance-readiness.json'
$result | ConvertTo-Json -Depth 10
if ($RequireReady -and -not $result.ready) {
    throw 'PITX WebPay fiscal issuance readiness is NOT READY.'
}
