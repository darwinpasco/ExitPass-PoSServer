using ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using System.Runtime.CompilerServices;
using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests;

public sealed class PostgresFiscalDocumentRepositoryTests
{
    [Fact]
    public void InsertSqlTargetsOnlyIdempotencyAndFiscalDocumentShellTables()
    {
        var sql = PostgresFiscalDocumentSql.SelectEligibleFiscalIdentity +
            PostgresFiscalDocumentSql.CountFiscalIdentityRelationships +
            PostgresFiscalDocumentSql.SelectEligibleFiscalSequencePolicy +
            PostgresFiscalDocumentSql.CountFiscalSequencePolicies +
            PostgresFiscalDocumentSql.SelectFiscalSequenceStateForUpdate +
            PostgresFiscalDocumentSql.CountFiscalSequenceStates +
            PostgresFiscalDocumentSql.UpdateFiscalSequenceStateIssued +
            PostgresFiscalDocumentSql.SelectReplayFiscalDocumentNumbering +
            PostgresFiscalDocumentSql.InsertIdempotencyRecord +
            PostgresFiscalDocumentSql.SelectIdempotencyRecordForUpdate +
            PostgresFiscalDocumentSql.UpdateIdempotencyRecordCompleted +
            PostgresFiscalDocumentSql.UpdateIdempotencyRecordReplay +
            PostgresFiscalDocumentSql.InsertFiscalDocument +
            PostgresFiscalDocumentSql.InsertFiscalDocumentStatusHistory +
            PostgresFiscalDocumentSql.InsertFiscalDocumentLink +
            PostgresFiscalDocumentSql.InsertFiscalDocumentLine +
            PostgresFiscalDocumentSql.InsertFiscalTender +
            PostgresFiscalDocumentSql.InsertFiscalTaxDetail +
            PostgresFiscalDocumentSql.InsertFiscalDiscountPrivilegeDetail +
            PostgresFiscalDocumentSql.InsertFiscalTotal +
            PostgresFiscalDocumentSql.InsertAppliedStatutoryFiscalFacts;

        Assert.Contains("pos.site_pos_server_fiscal_identity_history", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pos.fiscal_identities", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pos.fiscal_sequence_policies", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pos.fiscal_sequence_states", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pos.idempotency_records", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into pos.fiscal_documents", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into pos.fiscal_document_status_history", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into pos.fiscal_document_links", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into pos.fiscal_document_lines", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into pos.fiscal_tenders", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into pos.fiscal_tax_details", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into pos.fiscal_discount_privilege_details", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into pos.fiscal_totals", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into pos.fiscal_document_applied_statutory_facts", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("insert into pos.fiscal_sequence_policies", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("update pos.fiscal_sequence_policies", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("insert into pos.fiscal_sequence_states", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("insert into pos.fiscal_identities", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("update pos.fiscal_identities", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.fiscal_report", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.digital_si", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.audit", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.fiscal_counter_states", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exit", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gate", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InsertSqlUsesParameterizedValues()
    {
        var sql = PostgresFiscalDocumentSql.SelectEligibleFiscalIdentity +
            PostgresFiscalDocumentSql.CountFiscalIdentityRelationships +
            PostgresFiscalDocumentSql.SelectEligibleFiscalSequencePolicy +
            PostgresFiscalDocumentSql.CountFiscalSequencePolicies +
            PostgresFiscalDocumentSql.SelectFiscalSequenceStateForUpdate +
            PostgresFiscalDocumentSql.CountFiscalSequenceStates +
            PostgresFiscalDocumentSql.UpdateFiscalSequenceStateIssued +
            PostgresFiscalDocumentSql.SelectReplayFiscalDocumentNumbering +
            PostgresFiscalDocumentSql.InsertIdempotencyRecord +
            PostgresFiscalDocumentSql.SelectIdempotencyRecordForUpdate +
            PostgresFiscalDocumentSql.UpdateIdempotencyRecordCompleted +
            PostgresFiscalDocumentSql.UpdateIdempotencyRecordReplay +
            PostgresFiscalDocumentSql.InsertFiscalDocument +
            PostgresFiscalDocumentSql.InsertFiscalDocumentStatusHistory +
            PostgresFiscalDocumentSql.InsertFiscalDocumentLink +
            PostgresFiscalDocumentSql.InsertFiscalDocumentLine +
            PostgresFiscalDocumentSql.InsertFiscalTender +
            PostgresFiscalDocumentSql.InsertFiscalTaxDetail +
            PostgresFiscalDocumentSql.InsertFiscalDiscountPrivilegeDetail +
            PostgresFiscalDocumentSql.InsertFiscalTotal +
            PostgresFiscalDocumentSql.InsertAppliedStatutoryFiscalFacts +
            PostgresFiscalDocumentSql.SelectActiveControlledCodeIdBySetAndCode;
        var untrusted = "payable-basis-001'); drop table pos.fiscal_documents; --";

        Assert.Contains("@site_pos_server_id", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_document_type_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@idempotency_scope", sql, StringComparison.Ordinal);
        Assert.Contains("@idempotency_key", sql, StringComparison.Ordinal);
        Assert.Contains("@semantic_request_hash", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_sequence_policy_id", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_sequence_value", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_document_number", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_number_assigned_at", sql, StringComparison.Ordinal);
        Assert.Contains("@operation_type_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@operation_status_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@idempotency_context", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_document_id", sql, StringComparison.Ordinal);
        Assert.Contains("@site_pos_server_id", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_document_status_history_id", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_document_status_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@source_fiscal_document_id", sql, StringComparison.Ordinal);
        Assert.Contains("@target_fiscal_document_id", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_document_link_type_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_document_line_id", sql, StringComparison.Ordinal);
        Assert.Contains("@line_sequence", sql, StringComparison.Ordinal);
        Assert.Contains("@line_type_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@gross_amount_minor_units", sql, StringComparison.Ordinal);
        Assert.Contains("@line_context", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_tender_id", sql, StringComparison.Ordinal);
        Assert.Contains("@tender_type_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@amount_minor_units", sql, StringComparison.Ordinal);
        Assert.Contains("@tender_context", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_tax_detail_id", sql, StringComparison.Ordinal);
        Assert.Contains("@tax_type_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@tax_classification_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@taxable_amount_minor_units", sql, StringComparison.Ordinal);
        Assert.Contains("@tax_amount_minor_units", sql, StringComparison.Ordinal);
        Assert.Contains("@tax_context", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_discount_privilege_detail_id", sql, StringComparison.Ordinal);
        Assert.Contains("@discount_privilege_type_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@basis_amount_minor_units", sql, StringComparison.Ordinal);
        Assert.Contains("@discount_amount_minor_units", sql, StringComparison.Ordinal);
        Assert.Contains("@vat_privilege_amount_minor_units", sql, StringComparison.Ordinal);
        Assert.Contains("@discount_privilege_context", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_total_id", sql, StringComparison.Ordinal);
        Assert.Contains("@total_type_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@total_context", sql, StringComparison.Ordinal);
        Assert.Contains("@statutory_discount_decision_command_id", sql, StringComparison.Ordinal);
        Assert.Contains("@statutory_payable_basis_application_command_id", sql, StringComparison.Ordinal);
        Assert.Contains("@entitlement_type_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@benefit_classification_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@policy_resolution_basis_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@vat_treatment_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@source_payment_channel_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@code_set_key", sql, StringComparison.Ordinal);
        Assert.Contains("@code_key", sql, StringComparison.Ordinal);
        Assert.Contains("@document_context", sql, StringComparison.Ordinal);
        Assert.DoesNotContain(untrusted, sql, StringComparison.Ordinal);
        Assert.DoesNotContain("drop table", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IdempotencySqlLocksByScopeAndKeyWithoutUsingSequenceState()
    {
        var idempotencySql = PostgresFiscalDocumentSql.InsertIdempotencyRecord +
            PostgresFiscalDocumentSql.SelectIdempotencyRecordForUpdate +
            PostgresFiscalDocumentSql.UpdateIdempotencyRecordCompleted +
            PostgresFiscalDocumentSql.UpdateIdempotencyRecordReplay;

        Assert.Contains("on conflict (idempotency_scope, idempotency_key) do nothing", idempotencySql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("for update", idempotencySql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("linked_fiscal_document_id", idempotencySql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fiscal_sequence_states", idempotencySql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fiscal_sequence_value", idempotencySql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fiscal_document_number", idempotencySql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VoidSqlLocksDocumentUpdatesStatusHistoryAndDoesNotAllocateSequence()
    {
        var voidSql = PostgresFiscalDocumentSql.SelectFiscalDocumentForVoidUpdate +
            PostgresFiscalDocumentSql.SelectVoidFiscalDocumentStatusCode +
            PostgresFiscalDocumentSql.UpdateFiscalDocumentVoided +
            PostgresFiscalDocumentSql.InsertFiscalDocumentVoidStatusHistory;

        Assert.Contains("from pos.fiscal_documents", voidSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("for update of document", voidSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("update pos.fiscal_documents", voidSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("void_status = 'recorded'", voidSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("void_reason_code = @void_reason_code", voidSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("void_idempotency_key = @void_idempotency_key", voidSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("void_semantic_request_hash = @void_semantic_request_hash", voidSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into pos.fiscal_document_status_history", voidSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("prior_fiscal_document_status_code_id", voidSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@voided_fiscal_document_status_code_id", voidSql, StringComparison.Ordinal);
        Assert.DoesNotContain("update pos.fiscal_sequence_states", voidSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("last_issued_sequence_value", voidSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("current_sequence_value =", voidSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("insert into pos.fiscal_documents", voidSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.digital_si", voidSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.fiscal_report", voidSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exit", voidSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gate", voidSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VoidRepositoryUsesIdempotencyAndStatusTransitionInSingleTransaction()
    {
        var repositorySource = File.ReadAllText(FindRepositorySourcePath());

        Assert.Contains("PostgresFiscalDocumentSql.SelectFiscalDocumentForVoidUpdate,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertIdempotencyRecord,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.SelectIdempotencyRecordForUpdate,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.UpdateFiscalDocumentVoided,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertFiscalDocumentVoidStatusHistory,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.UpdateIdempotencyRecordCompleted,", repositorySource, StringComparison.Ordinal);

        var voidLockIndex = repositorySource.IndexOf("var lockedDocument = await ReadFiscalDocumentForVoidUpdateAsync", StringComparison.Ordinal);
        var statusResolveIndex = repositorySource.IndexOf("var voidedStatusCodeId = await ResolveVoidedStatusCodeIdAsync", StringComparison.Ordinal);
        var idempotencyInsertIndex = repositorySource.LastIndexOf("InsertIdempotencyRecord,", StringComparison.Ordinal);
        var idempotencyLockIndex = repositorySource.LastIndexOf("SelectIdempotencyRecordForUpdate,", StringComparison.Ordinal);
        var updateIndex = repositorySource.IndexOf("UpdateFiscalDocumentVoided,", StringComparison.Ordinal);
        var historyIndex = repositorySource.IndexOf("InsertFiscalDocumentVoidStatusHistory,", StringComparison.Ordinal);
        var completeIndex = repositorySource.LastIndexOf("UpdateIdempotencyRecordCompleted,", StringComparison.Ordinal);

        Assert.True(voidLockIndex < statusResolveIndex);
        Assert.True(statusResolveIndex < idempotencyInsertIndex);
        Assert.True(idempotencyInsertIndex < idempotencyLockIndex);
        Assert.True(idempotencyLockIndex < updateIndex);
        Assert.True(updateIndex < historyIndex);
        Assert.True(historyIndex < completeIndex);
    }

    [Fact]
    public void FiscalIdentityResolutionSqlUsesSiteHistoryAndEffectiveActiveFiltersOnly()
    {
        var identitySql = PostgresFiscalDocumentSql.SelectEligibleFiscalIdentity +
            PostgresFiscalDocumentSql.CountFiscalIdentityRelationships;

        Assert.Contains("pos.site_pos_server_fiscal_identity_history", identitySql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pos.fiscal_identities", identitySql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("history.effective_start_at <= current_timestamp", identitySql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("history.effective_end_at is null", identitySql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("site.is_active = true", identitySql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("identity.is_active = true", identitySql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("channel_terminals", identitySql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fiscal_sequence_states", identitySql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("for update", identitySql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FiscalSequencePolicyResolutionSqlUsesSiteDocumentTypeAndEffectiveStatusFiltersOnly()
    {
        var policySql = PostgresFiscalDocumentSql.SelectEligibleFiscalSequencePolicy +
            PostgresFiscalDocumentSql.CountFiscalSequencePolicies;

        Assert.Contains("pos.fiscal_sequence_policies", policySql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("policy.site_pos_server_id = @site_pos_server_id", policySql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("policy.document_type_code_id = @fiscal_document_type_code_id", policySql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("policy.effective_start_at <= current_timestamp", policySql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("policy.effective_end_at is null", policySql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("policy_status.is_active = true", policySql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fiscal_sequence_states", policySql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("for update", policySql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("update ", policySql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HeaderInsertPersistsIdentityAndFiscalNumberingFields()
    {
        var headerSql = PostgresFiscalDocumentSql.InsertFiscalDocument;

        Assert.Contains("fiscal_identity_id", headerSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_sequence_policy_id", headerSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_sequence_value", headerSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_document_number", headerSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_series", headerSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_number_prefix_text", headerSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_number_suffix_text", headerSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_number_assigned_at", headerSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_number_assigned_by_ref", headerSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fiscal_sequence_states", headerSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fiscal_counter_states", headerSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FiscalDocumentSchemaPersistsVoidFactsWithoutChangingNumberColumns()
    {
        var schema = File.ReadAllText(FindTableSourcePath("pos.fiscal_documents.sql"));

        Assert.Contains("void_status text", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("void_reason_code text", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("void_requested_by_ref text", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("void_idempotency_key text", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("void_semantic_request_hash text", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("void_correlation_id text", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("void_record_consistency", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_document_number text", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_sequence_value bigint", schema, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SequenceAllocationSqlLocksAndUpdatesOnlySequenceState()
    {
        var sequenceSql = PostgresFiscalDocumentSql.SelectFiscalSequenceStateForUpdate +
            PostgresFiscalDocumentSql.UpdateFiscalSequenceStateIssued;

        Assert.Contains("from pos.fiscal_sequence_states", sequenceSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("for update", sequenceSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("update pos.fiscal_sequence_states", sequenceSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("current_sequence_value = @fiscal_sequence_value", sequenceSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("last_issued_sequence_value = @fiscal_sequence_value", sequenceSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.fiscal_counter_states", sequenceSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.fiscal_sequence_gap_audit", sequenceSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.fiscal_documents", sequenceSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exit", sequenceSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gate", sequenceSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StatusHistoryUsesSameStatusCodeParameterAsHeader()
    {
        var headerSql = PostgresFiscalDocumentSql.InsertFiscalDocument;
        var historySql = PostgresFiscalDocumentSql.InsertFiscalDocumentStatusHistory;

        Assert.Contains("@fiscal_document_status_code_id", headerSql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_document_status_code_id", historySql, StringComparison.Ordinal);
        Assert.Contains("prior_fiscal_document_status_code_id", historySql, StringComparison.OrdinalIgnoreCase);
        Assert.Matches(@"prior_fiscal_document_status_code_id[\s\S]+null[\s\S]+@fiscal_document_status_code_id", historySql);
    }

    [Fact]
    public void LinkSqlUsesDocumentToDocumentMapping()
    {
        var linkSql = PostgresFiscalDocumentSql.InsertFiscalDocumentLink;

        Assert.Contains("source_fiscal_document_id", linkSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("target_fiscal_document_id", linkSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_document_link_type_code_id", linkSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@created_by_ref", linkSql, StringComparison.Ordinal);
        Assert.DoesNotContain("payment_finality", linkSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("discount_validation", linkSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exit", linkSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gate", linkSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LineSqlUsesFiscalLineMappingOnly()
    {
        var lineSql = PostgresFiscalDocumentSql.InsertFiscalDocumentLine;

        Assert.Contains("line_sequence", lineSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("line_type_code_id", lineSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("quantity", lineSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gross_amount_minor_units", lineSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("net_amount_minor_units", lineSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tender", lineSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tax_detail", lineSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("discount_privilege", lineSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fiscal_total", lineSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exit", lineSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gate", lineSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TenderSqlUsesFiscalTenderMappingOnly()
    {
        var tenderSql = PostgresFiscalDocumentSql.InsertFiscalTender;

        Assert.Contains("tender_type_code_id", tenderSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("amount_minor_units", tenderSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("central_pms_payment_attempt_ref", tenderSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("central_pms_payment_confirmation_ref", tenderSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("payment_finality_ref", tenderSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("provider_ref", tenderSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tax_detail", tenderSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("discount_privilege", tenderSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fiscal_total", tenderSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exit", tenderSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gate", tenderSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TaxDetailSqlUsesFiscalTaxDetailMappingOnly()
    {
        var taxSql = PostgresFiscalDocumentSql.InsertFiscalTaxDetail;

        Assert.Contains("pos.fiscal_tax_details", taxSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_document_id", taxSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_document_line_id", taxSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tax_type_code_id", taxSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tax_classification_code_id", taxSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("taxable_amount_minor_units", taxSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tax_amount_minor_units", taxSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("currency_code", taxSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("discount_privilege", taxSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fiscal_total", taxSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("report", taxSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exit", taxSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gate", taxSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DiscountPrivilegeDetailSqlUsesFiscalDiscountPrivilegeMappingOnly()
    {
        var discountSql = PostgresFiscalDocumentSql.InsertFiscalDiscountPrivilegeDetail;

        Assert.Contains("pos.fiscal_discount_privilege_details", discountSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_document_id", discountSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_document_line_id", discountSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("discount_privilege_type_code_id", discountSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("basis_amount_minor_units", discountSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("discount_amount_minor_units", discountSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("vat_privilege_amount_minor_units", discountSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("currency_code", discountSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("beneficiary_ref", discountSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("evidence_ref", discountSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("approval_ref", discountSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fiscal_total", discountSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("report", discountSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exit", discountSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gate", discountSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TotalSqlUsesFiscalTotalMappingOnly()
    {
        var totalSql = PostgresFiscalDocumentSql.InsertFiscalTotal;

        Assert.Contains("pos.fiscal_totals", totalSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_document_id", totalSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("total_type_code_id", totalSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("amount_minor_units", totalSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("currency_code", totalSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("total_context", totalSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("report", totalSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("digital_si", totalSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("annex", totalSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("x_z", totalSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exit", totalSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gate", totalSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RepositoryUsesSingleTransactionForIdempotencySequenceAllocationHeaderStatusHistoryLinksLinesTendersTaxDiscountPrivilegeTotalsAndSequenceUpdate()
    {
        var repositorySource = File.ReadAllText(FindRepositorySourcePath());

        Assert.Contains("BeginTransactionAsync", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertIdempotencyRecord,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.SelectIdempotencyRecordForUpdate,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.SelectFiscalSequenceStateForUpdate,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertFiscalDocument,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertFiscalDocumentStatusHistory,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertFiscalDocumentLink,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertFiscalDocumentLine,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertFiscalTender,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertFiscalTaxDetail,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertFiscalDiscountPrivilegeDetail,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertFiscalTotal,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.UpdateFiscalSequenceStateIssued,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("CommitAsync", repositorySource, StringComparison.Ordinal);
        Assert.Contains("RollbackAsync", repositorySource, StringComparison.Ordinal);

        var idempotencyInsertIndex = repositorySource.IndexOf("InsertIdempotencyRecord,", StringComparison.Ordinal);
        var idempotencyLockIndex = repositorySource.IndexOf("SelectIdempotencyRecordForUpdate,", StringComparison.Ordinal);
        var sequenceAllocationIndex = repositorySource.IndexOf("var assignment = await AllocateFiscalNumberAsync", StringComparison.Ordinal);
        var headerIndex = repositorySource.IndexOf("InsertFiscalDocument,", StringComparison.Ordinal);
        var statusIndex = repositorySource.IndexOf("InsertFiscalDocumentStatusHistory,", StringComparison.Ordinal);
        var linkIndex = repositorySource.IndexOf("InsertFiscalDocumentLink,", StringComparison.Ordinal);
        var lineIndex = repositorySource.IndexOf("InsertFiscalDocumentLine,", StringComparison.Ordinal);
        var tenderIndex = repositorySource.IndexOf("InsertFiscalTender,", StringComparison.Ordinal);
        var taxIndex = repositorySource.IndexOf("InsertFiscalTaxDetail,", StringComparison.Ordinal);
        var discountIndex = repositorySource.IndexOf("InsertFiscalDiscountPrivilegeDetail,", StringComparison.Ordinal);
        var totalIndex = repositorySource.IndexOf("InsertFiscalTotal,", StringComparison.Ordinal);
        var sequenceUpdateIndex = repositorySource.IndexOf("UpdateFiscalSequenceStateIssued,", StringComparison.Ordinal);
        var completeIdempotencyIndex = repositorySource.IndexOf("UpdateIdempotencyRecordCompleted,", StringComparison.Ordinal);
        var commitIndex = repositorySource.LastIndexOf("CommitAsync", StringComparison.Ordinal);
        Assert.True(idempotencyInsertIndex < idempotencyLockIndex);
        Assert.True(idempotencyLockIndex < sequenceAllocationIndex);
        Assert.True(sequenceAllocationIndex < headerIndex);
        Assert.True(headerIndex < statusIndex);
        Assert.True(statusIndex < linkIndex);
        Assert.True(linkIndex < lineIndex);
        Assert.True(lineIndex < tenderIndex);
        Assert.True(tenderIndex < taxIndex);
        Assert.True(taxIndex < discountIndex);
        Assert.True(discountIndex < totalIndex);
        Assert.True(totalIndex < sequenceUpdateIndex);
        Assert.True(sequenceUpdateIndex < completeIdempotencyIndex);
        Assert.True(completeIdempotencyIndex < commitIndex);
    }

    [Fact]
    public void DocumentContextPreservesReferencesWithoutRawEvidence()
    {
        var draft = ValidDraft();

        var json = PostgresFiscalDocumentSql.CreateDocumentContextJson(draft);

        Assert.Contains("payable-basis-001", json, StringComparison.Ordinal);
        Assert.Contains("central-finality-001", json, StringComparison.Ordinal);
        Assert.Contains("discount-validation-001", json, StringComparison.Ordinal);
        Assert.Contains("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee", json, StringComparison.Ordinal);
        Assert.Contains("line-source-001", json, StringComparison.Ordinal);
        Assert.Contains("provider-ref-001", json, StringComparison.Ordinal);
        Assert.Contains("33333333-3333-3333-3333-333333333333", json, StringComparison.Ordinal);
        Assert.Contains("55555555-5555-5555-5555-555555555555", json, StringComparison.Ordinal);
        Assert.Contains("discount-validation-001", json, StringComparison.Ordinal);
        Assert.Contains("66666666-6666-6666-6666-666666666666", json, StringComparison.Ordinal);
        Assert.Contains("SI-00000001-A", json, StringComparison.Ordinal);
        Assert.Contains("allocated_in_document_transaction", json, StringComparison.Ordinal);
        Assert.DoesNotContain("raw_id", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("evidence_payload", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DocumentContextPersistsNormalizedInvoiceCustomerInformation()
    {
        var draft = ValidDraft() with
        {
            InvoiceCustomerInformation = new InvoiceCustomerInformationSnapshot(
                "Juan Dela Cruz",
                "100 Sample Street",
                "123-456-789",
                "Sample Trading",
                null)
        };

        var json = PostgresFiscalDocumentSql.CreateDocumentContextJson(draft);
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var customer = document.RootElement.GetProperty("invoice_customer_information");

        Assert.Equal("Juan Dela Cruz", customer.GetProperty("customer_name").GetString());
        Assert.Equal("100 Sample Street", customer.GetProperty("address").GetString());
        Assert.Equal("123-456-789", customer.GetProperty("tin").GetString());
        Assert.Equal("Sample Trading", customer.GetProperty("business_style").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, customer.GetProperty("statutory_id_number").ValueKind);
    }

    [Fact]
    public void CustomerInformationIdempotencyContextUsesAdditiveSha256V4()
    {
        var draft = ValidDraft() with
        {
            InvoiceCustomerInformation = new InvoiceCustomerInformationSnapshot(
                "Juan Dela Cruz", null, null, null, null)
        };
        var idempotency = new FiscalIssuanceIdempotency("scope", "key", new string('c', 64));

        var json = PostgresFiscalDocumentSql.CreateIdempotencyContextJson(draft, idempotency);

        Assert.Contains(FiscalDocumentSemanticRequestHasher.InvoiceCustomerInformationVersion, json, StringComparison.Ordinal);
    }

    [Fact]
    public void IdempotencyContextDocumentsStableSourceKeyAndSemanticHashVersion()
    {
        var draft = ValidDraft();
        var idempotency = new FiscalIssuanceIdempotency(
            "fiscal_document_creation:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb:cccccccccccccccccccccccccccccccc",
            "central-finality-001",
            new string('a', 64));

        var json = PostgresFiscalDocumentSql.CreateIdempotencyContextJson(draft, idempotency);

        Assert.Contains("idempotency_key_source", json, StringComparison.Ordinal);
        Assert.Contains("upstream_finality_ref", json, StringComparison.Ordinal);
        Assert.Contains("semantic_request_hash_version", json, StringComparison.Ordinal);
        Assert.Contains("sha256:v1", json, StringComparison.Ordinal);
        Assert.Contains("semantic_request_hash_status", json, StringComparison.Ordinal);
        Assert.Contains("calculated", json, StringComparison.Ordinal);
        Assert.DoesNotContain("payment_payload", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StatutoryIdempotencyContextDocumentsSha256V2WithoutChangingOrdinaryV1()
    {
        var idempotency = new FiscalIssuanceIdempotency(
            "fiscal_document_creation:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb:cccccccccccccccccccccccccccccccc",
            "central-finality-001",
            new string('b', 64));

        var ordinaryJson = PostgresFiscalDocumentSql.CreateIdempotencyContextJson(ValidDraft(), idempotency);
        var statutoryJson = PostgresFiscalDocumentSql.CreateIdempotencyContextJson(ValidStatutoryDraft(), idempotency);

        Assert.Contains("sha256:v1", ordinaryJson, StringComparison.Ordinal);
        Assert.Contains("pos-server-fiscal-document-create:sha256:v2", statutoryJson, StringComparison.Ordinal);
        Assert.DoesNotContain("beneficiary_name", statutoryJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("evidence_image", statutoryJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LineContextPreservesReferenceOnlyJson()
    {
        var lineContext = PostgresFiscalDocumentSql.CreateLineContextJson(ValidDraft().DocumentLines[0]);

        Assert.NotNull(lineContext);
        Assert.Contains("source_system", lineContext, StringComparison.Ordinal);
        Assert.DoesNotContain("raw_id", lineContext, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("evidence_payload", lineContext, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TenderContextPreservesReferenceOnlyJson()
    {
        var tenderContext = PostgresFiscalDocumentSql.CreateTenderContextJson(ValidDraft().Tenders[0]);

        Assert.NotNull(tenderContext);
        Assert.Contains("source_system", tenderContext, StringComparison.Ordinal);
        Assert.DoesNotContain("card_number", tenderContext, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payment_payload", tenderContext, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TaxContextPreservesReferenceOnlyJson()
    {
        var taxContext = PostgresFiscalDocumentSql.CreateTaxContextJson(ValidDraft().TaxDetails[0]);

        Assert.NotNull(taxContext);
        Assert.Contains("source_system", taxContext, StringComparison.Ordinal);
        Assert.DoesNotContain("raw_id", taxContext, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payment_payload", taxContext, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DiscountPrivilegeContextPreservesReferenceOnlyJson()
    {
        var context = PostgresFiscalDocumentSql.CreateDiscountPrivilegeContextJson(ValidDraft().DiscountPrivilegeDetails[0]);

        Assert.NotNull(context);
        Assert.Contains("source_system", context, StringComparison.Ordinal);
        Assert.DoesNotContain("raw_id", context, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("evidence_payload", context, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TotalContextPreservesReferenceOnlyJson()
    {
        var context = PostgresFiscalDocumentSql.CreateTotalContextJson(ValidDraft().Totals[0]);

        Assert.NotNull(context);
        Assert.Contains("source_system", context, StringComparison.Ordinal);
        Assert.DoesNotContain("raw_id", context, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payment_payload", context, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicPersistenceAssemblyDoesNotExposeAuthorityLeakingBehavior()
    {
        var forbiddenNames = new[]
        {
            "Entitlement",
            "OperatorConsole",
            "LocalOrdinance",
            "FinalizePayment",
            "PaymentLifecycle",
            "ExitAuthorization",
            "GateExecution",
            "FiscalTax",
            "FiscalTotal"
        };

        foreach (var type in typeof(PostgresFiscalDocumentRepository).Assembly.GetTypes())
        {
            foreach (var forbiddenName in forbiddenNames)
            {
                Assert.DoesNotContain(forbiddenName, type.Name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static FiscalDocumentDraft ValidDraft() =>
        new(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("99999999-9999-9999-9999-999999999999"),
            null,
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            "site-pos-server-001",
            "sales_invoice",
            "runtime-terminal-001",
            "payable-basis-001",
            "central-finality-001",
            "PHP",
            12500,
            DateOnly.FromDateTime(DateTime.UtcNow.Date),
            "parking-session-001",
            "payment-attempt-001",
            "payment-confirmation-001",
            "central-finality-001",
            "vendor-ack-001",
            [
                new FiscalDocumentLinkInput(
                    Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                    CreatedByRef: "pos-server-persistence-test")
            ],
            [
                new FiscalDocumentLineInput(
                    1,
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "Parking fee",
                    1,
                    12500,
                    12500,
                    0,
                    0,
                    12500,
                    "PHP",
                    SourceRef: "line-source-001",
                    LineContext: new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            [
                new FiscalTenderInput(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    12500,
                    "PHP",
                    CentralPmsPaymentAttemptRef: "payment-attempt-001",
                    CentralPmsPaymentConfirmationRef: "payment-confirmation-001",
                    PaymentFinalityRef: "central-finality-001",
                    ProviderRef: "provider-ref-001",
                    TenderContext: new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            [
                new FiscalTaxDetailInput(
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    12500,
                    0,
                    "PHP",
                    LineSequence: 1,
                    TaxRate: 0,
                    TaxContext: new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            [
                new FiscalDiscountPrivilegeDetailInput(
                    Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    12500,
                    1000,
                    0,
                    "PHP",
                    LineSequence: 1,
                    BeneficiaryRef: "beneficiary-ref-001",
                    EvidenceRef: "evidence-ref-001",
                    ApprovalRef: "discount-validation-001",
                    DiscountPrivilegeContext: new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            [
                new FiscalTotalInput(
                    Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    12500,
                    "PHP",
                    new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            [new FiscalDiscountReferenceInput("discount-validation-001", FiscalDiscountReferenceStatus.Approved, true)],
            ResolvedFiscalIdentityId: Guid.Parse("77777777-7777-7777-7777-777777777777"),
            ResolvedFiscalSequencePolicyId: Guid.Parse("88888888-8888-8888-8888-888888888888"),
            FiscalSequenceValue: 1,
            FiscalDocumentNumber: "SI-00000001-A",
            FiscalSeries: "sales_invoice_policy",
            FiscalNumberPrefixText: "SI-",
            FiscalNumberSuffixText: "-A",
            FiscalNumberAssignedAt: DateTimeOffset.Parse("2026-07-01T00:00:00Z"),
            FiscalNumberAssignedByRef: "pos-server:system");

    private static FiscalDocumentDraft ValidStatutoryDraft() =>
        ValidDraft() with
        {
            PayableAmountMinorUnits = 7143,
            CentralPmsParkingSessionRef = "21000000-0000-4000-8000-000000000005",
            SiteId = Guid.Parse("21000000-0000-4000-8000-000000000006"),
            DocumentLines =
            [
                ValidDraft().DocumentLines[0] with
                {
                    UnitAmountMinorUnits = 10000,
                    GrossAmountMinorUnits = 10000,
                    DiscountAmountMinorUnits = 2857,
                    TaxAmountMinorUnits = 0,
                    NetAmountMinorUnits = 7143
                }
            ],
            Tenders = [ValidDraft().Tenders[0] with { AmountMinorUnits = 7143 }],
            TaxDetails = [ValidDraft().TaxDetails[0] with { TaxableAmountMinorUnits = 8929, TaxAmountMinorUnits = 0 }],
            DiscountPrivilegeDetails =
            [
                ValidDraft().DiscountPrivilegeDetails[0] with
                {
                    BasisAmountMinorUnits = 10000,
                    DiscountAmountMinorUnits = 1786,
                    VatPrivilegeAmountMinorUnits = 1071
                }
            ],
            Totals = [ValidDraft().Totals[0] with { AmountMinorUnits = 7143 }],
            AppliedStatutoryFiscalFacts = ValidAppliedStatutoryFiscalFactsSnapshot()
        };

    private static AppliedStatutoryFiscalFactsSnapshot ValidAppliedStatutoryFiscalFactsSnapshot() =>
        new(
            Guid.Parse("21000000-0000-4000-8000-000000000001"),
            Guid.Parse("21000000-0000-4000-8000-000000000002"),
            Guid.Parse("21000000-0000-4000-8000-000000000003"),
            Guid.Parse("21000000-0000-4000-8000-000000000004"),
            Guid.Parse("21000000-0000-4000-8000-000000000005"),
            Guid.Parse("21000000-0000-4000-8000-000000000006"),
            Guid.Parse("21000000-0000-4000-8000-000000000007"),
            "SENIOR_CITIZEN",
            "VAT_EXEMPTION_AND_STATUTORY_DISCOUNT",
            new AppliedStatutoryPolicyReferenceSnapshot(
                "NATIONAL_LAW",
                AppliedPolicyReferenceId: Guid.Parse("21000000-0000-4000-8000-000000000008"),
                PolicyCode: "TEST-SENIOR-CITIZEN-POLICY",
                NationalLawReference: "TEST-NATIONAL-LAW-REFERENCE"),
            Guid.Parse("21000000-0000-4000-8000-000000000009"),
            Guid.Parse("21000000-0000-4000-8000-000000000010"),
            10000,
            8929,
            0,
            "VAT_EXEMPT",
            1786,
            7143,
            "PHP",
            DateTimeOffset.Parse("2026-07-29T08:15:00+08:00"),
            "WEBPAY",
            SnapshotCreatedAt: DateTimeOffset.Parse("2026-07-29T08:16:00+08:00"));

    private static string FindRepositorySourcePath([CallerFilePath] string testFilePath = "")
    {
        var testSourceDirectory = Path.GetDirectoryName(testFilePath) ?? string.Empty;
        foreach (var startDirectory in new[] { testSourceDirectory, AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(startDirectory);
            while (current is not null)
            {
                var candidate = Path.Combine(
                    current.FullName,
                    "src",
                    "ExitPass.PosServer.Persistence.Postgres",
                    "FiscalDocuments",
                    "PostgresFiscalDocumentRepository.cs");

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }
        }

        throw new FileNotFoundException("Could not locate PostgresFiscalDocumentRepository.cs.");
    }

    private static string FindTableSourcePath(string fileName, [CallerFilePath] string testFilePath = "")
    {
        var testSourceDirectory = Path.GetDirectoryName(testFilePath) ?? string.Empty;
        foreach (var startDirectory in new[] { testSourceDirectory, AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(startDirectory);
            while (current is not null)
            {
                var candidate = Path.Combine(
                    current.FullName,
                    "db",
                    "state",
                    "tables",
                    fileName);

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }
        }

        throw new FileNotFoundException($"Could not locate {fileName}.");
    }
}
