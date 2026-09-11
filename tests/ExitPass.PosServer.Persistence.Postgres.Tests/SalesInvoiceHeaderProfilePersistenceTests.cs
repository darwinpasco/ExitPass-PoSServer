using ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;
using System.Runtime.CompilerServices;
using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests;

public sealed class SalesInvoiceHeaderProfilePersistenceTests
{
    [Fact]
    public void PersistentDatabaseReconciliationLimitsMutationsToApprovedCanonicalSources()
    {
        var reconciliation = File.ReadAllText(FindDatabaseScriptPath("Invoke-PosPersistentStateReconciliation.ps1"));

        Assert.Contains("db\\state\\tables\\pos.fiscal_documents.sql", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("db\\state\\tables\\pos.electronic_journal_records.sql", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("db\\state\\tables\\pos.fiscal_document_reporting_period_assignment.sql", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Get-StateColumnDefinition", reconciliation, StringComparison.Ordinal);
        Assert.Contains("Get-StateConstraintDefinition", reconciliation, StringComparison.Ordinal);
        Assert.Contains("Get-StateFunctionDefinition", reconciliation, StringComparison.Ordinal);
        Assert.Contains("Get-StateFunctionComment", reconciliation, StringComparison.Ordinal);
        Assert.Contains("Get-StateTriggerDefinition", reconciliation, StringComparison.Ordinal);
        Assert.Contains("function_definition_aligned", reconciliation, StringComparison.Ordinal);
        Assert.Contains("trigger_definition_aligned", reconciliation, StringComparison.Ordinal);
        Assert.Contains("apply_would_replace_function", reconciliation, StringComparison.Ordinal);
        Assert.Contains("apply_would_replace_trigger", reconciliation, StringComparison.Ordinal);
        Assert.Contains("ADD COLUMN IF NOT EXISTS", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SET NOT NULL", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE pos.fiscal_document_header_snapshots", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE pos.sales_invoice_header_profiles", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ALTER TABLE pos.sales_invoice_header_profiles", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ALTER TABLE pos.fiscal_document_header_snapshots", reconciliation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PersistentDatabaseReconciliationUsesProvenCompletionAncestryAndNeverReconstructsEjText()
    {
        var reconciliation = File.ReadAllText(FindDatabaseScriptPath("Invoke-PosPersistentStateReconciliation.ps1"));

        Assert.Contains("db\\state\\tables\\pos.fiscal_documents.sql", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("db\\state\\tables\\pos.electronic_journal_records.sql", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BLOCKED_NONDETERMINISTIC_COMPLETION_ANCESTRY_BACKFILL", reconciliation, StringComparison.Ordinal);
        Assert.Contains("BLOCKED_UNEXPECTED_DATABASE_INVENTORY_DRIFT", reconciliation, StringComparison.Ordinal);
        Assert.Contains("payment_finality_ref = d.document_context ->> 'upstream_finality_ref'", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("completion_authority_ref = COALESCE(completion_authority_ref, payment_finality_ref)", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ALTER COLUMN completion_basis SET NOT NULL", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ck_fiscal_documents__completion_basis", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ck_fiscal_documents__completion_ancestry", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ck_ej_records__printable_sales_invoice", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ck_ej_records__canonical_required", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BLOCKED_UNSUPPORTED_HISTORICAL_EJ_SEMANTIC_VERSION", reconciliation, StringComparison.Ordinal);
        Assert.Contains("DROP CONSTRAINT $(Quote-Identifier $canonicalRequiredConstraint)", reconciliation, StringComparison.Ordinal);
        Assert.Contains("pos-server-electronic-journal-event-semantic:sha256:v1", reconciliation, StringComparison.Ordinal);
        Assert.Contains("pos-server-electronic-journal-event-semantic:sha256:v2", reconciliation, StringComparison.Ordinal);
        Assert.Contains("'semantic_hash_version', semantic_hash_version", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'printable_sales_invoice_text', to_jsonb(e)", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("printable_sales_invoice_text IS NOT NULL", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE pos.electronic_journal_records", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", reconciliation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PersistentDatabaseReconciliationRejectsUnapprovedObjectInventoryDrift()
    {
        var reconciliation = File.ReadAllText(FindDatabaseScriptPath("Invoke-PosPersistentStateReconciliation.ps1"));

        Assert.Contains("db\\validation\\pos_expected_inventory.json", reconciliation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Get-DatabaseObjectInventoryDrift", reconciliation, StringComparison.Ordinal);
        Assert.Contains("missing unexpected", reconciliation, StringComparison.Ordinal);
        Assert.Contains("extra unexpected", reconciliation, StringComparison.Ordinal);
        Assert.Contains("BLOCKED_UNEXPECTED_DATABASE_INVENTORY_DRIFT", reconciliation, StringComparison.Ordinal);
    }

    [Fact]
    public void SalesInvoiceHeaderProfileSchemaContainsRequiredLifecycleDatesAndVersionConstraints()
    {
        var schema = File.ReadAllText(FindTableSourcePath("pos.sales_invoice_header_profiles.sql"));

        Assert.Contains("sales_invoice_header_profile_id uuid not null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_identity_id uuid not null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("site_id uuid not null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("site_pos_server_id uuid not null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("profile_version text not null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("template_version = 'digital-sales-invoice-json-v1'", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("presentation_version = 'digital-sales-invoice-presentation-json-v1'", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bir_accreditation_issued_date date null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bir_accreditation_valid_until date null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ptu_issued_date date null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("supplier_developer_registered_name text null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("supplier_developer_address text null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("supplier_developer_tin text null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ck_sales_invoice_header_profiles__approved_completeness", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lifecycle_status <> 'APPROVED' OR", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lifecycle_status in ('DRAFT', 'APPROVED', 'RETIRED')", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("effective_to is null or effective_to > effective_from", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bir_accreditation_valid_until >= bir_accreditation_issued_date", schema, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FiscalDocumentHeaderSnapshotSchemaIsImmutableOneToOneAndContainsRuntimeTerminal()
    {
        var schema = File.ReadAllText(FindTableSourcePath("pos.fiscal_document_header_snapshots.sql"));

        Assert.Contains("fiscal_document_id uuid not null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sales_invoice_header_profile_id uuid not null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("registered_business_name text not null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tin text not null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("terminal_id text null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("supplier_developer_registered_name text not null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("supplier_developer_address text not null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("supplier_developer_tin text not null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bir_accreditation_issued_date date not null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bir_accreditation_valid_until date not null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ptu_issued_date date not null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("snapshot_json jsonb not null", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unique (fiscal_document_id)", schema, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("print_job", schema, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exit_authorization", schema, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gate", schema, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IssuanceSqlResolvesAndSnapshotsHeaderProfileInsideTransaction()
    {
        var repositorySource = File.ReadAllText(FindRepositorySourcePath());
        var sql = PostgresFiscalDocumentSql.SelectEffectiveSalesInvoiceHeaderProfileForUpdate +
            PostgresFiscalDocumentSql.InsertFiscalDocumentHeaderSnapshot +
            PostgresFiscalDocumentSql.SelectReplayFiscalDocumentHeaderSnapshot;

        Assert.Contains("from pos.sales_invoice_header_profiles profile", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("for update of profile", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into pos.fiscal_document_header_snapshots", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@terminal_id", sql, StringComparison.Ordinal);
        Assert.Contains("snapshot_json", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ResolveSalesInvoiceHeaderSnapshotAsync", repositorySource, StringComparison.Ordinal);
        Assert.Contains("InsertFiscalDocumentHeaderSnapshot", repositorySource, StringComparison.Ordinal);

        var resolveIndex = repositorySource.IndexOf("ResolveSalesInvoiceHeaderSnapshotAsync", StringComparison.Ordinal);
        var documentIndex = repositorySource.IndexOf("InsertFiscalDocument,", StringComparison.Ordinal);
        var snapshotIndex = repositorySource.IndexOf("InsertFiscalDocumentHeaderSnapshot,", StringComparison.Ordinal);
        var commitIndex = repositorySource.LastIndexOf("CommitAsync", StringComparison.Ordinal);
        Assert.True(resolveIndex < documentIndex);
        Assert.True(documentIndex < snapshotIndex);
        Assert.True(snapshotIndex < commitIndex);
    }

    [Fact]
    public void IdempotentReplayReturnsBeforeProfileResolutionAndSnapshotCreation()
    {
        var repositorySource = File.ReadAllText(FindRepositorySourcePath());

        var selectIdempotencyIndex = repositorySource.IndexOf("SelectIdempotencyRecordForUpdate", StringComparison.Ordinal);
        var semanticConflictIndex = repositorySource.IndexOf("existingSemanticHash, idempotency.SemanticRequestHash", StringComparison.Ordinal);
        var replayBranchIndex = repositorySource.IndexOf("if (linkedFiscalDocumentId is not null)", StringComparison.Ordinal);
        var replayReadIndex = repositorySource.IndexOf("ReadReplayDraftAsync", StringComparison.Ordinal);
        var replayReturnIndex = repositorySource.IndexOf("return FiscalDocumentPersistenceResult.Replayed(replayedDraft)", StringComparison.Ordinal);
        var resolveFiscalContextIndex = repositorySource.IndexOf("ResolveFiscalContextAsync", StringComparison.Ordinal);
        var allocateFiscalNumberIndex = repositorySource.IndexOf("AllocateFiscalNumberAsync", StringComparison.Ordinal);
        var resolveHeaderSnapshotIndex = repositorySource.IndexOf("ResolveSalesInvoiceHeaderSnapshotAsync", StringComparison.Ordinal);
        var insertSnapshotIndex = repositorySource.IndexOf("InsertFiscalDocumentHeaderSnapshot", StringComparison.Ordinal);

        Assert.True(selectIdempotencyIndex >= 0);
        Assert.True(semanticConflictIndex > selectIdempotencyIndex);
        Assert.True(replayBranchIndex > semanticConflictIndex);
        Assert.True(replayReadIndex > replayBranchIndex);
        Assert.True(replayReturnIndex > replayReadIndex);
        Assert.True(replayReturnIndex < resolveFiscalContextIndex);
        Assert.True(replayReturnIndex < allocateFiscalNumberIndex);
        Assert.True(replayReturnIndex < resolveHeaderSnapshotIndex);
        Assert.True(resolveHeaderSnapshotIndex < insertSnapshotIndex);
        Assert.DoesNotContain("SalesInvoiceHeaderProfile", FiscalDocumentSemanticRequestHasherSource(), StringComparison.Ordinal);
    }

    [Fact]
    public void ProfileRepositoryDoesNotExposeDeleteOrExternalWorkflowBehavior()
    {
        var source = File.ReadAllText(FindProfileRepositorySourcePath());

        Assert.Contains("CreateFiscalIdentityAsync", source, StringComparison.Ordinal);
        Assert.Contains("UpdateFiscalIdentityAsync", source, StringComparison.Ordinal);
        Assert.Contains("IsFiscalIdentityInGovernedUseAsync", source, StringComparison.Ordinal);
        Assert.Contains("CreateHeaderProfileAsync", source, StringComparison.Ordinal);
        Assert.Contains("ListHeaderProfilesAsync", source, StringComparison.Ordinal);
        Assert.Contains("UpdateHeaderProfileDraftAsync", source, StringComparison.Ordinal);
        Assert.Contains("ApproveHeaderProfileAsync", source, StringComparison.Ordinal);
        Assert.Contains("RetireHeaderProfileAsync", source, StringComparison.Ordinal);
        Assert.Contains("GetHeaderProfileUsageAsync", source, StringComparison.Ordinal);
        Assert.Contains("lifecycle_status = 'DRAFT'", source, StringComparison.Ordinal);
        Assert.Contains("lifecycle_status = 'RETIRED'", source, StringComparison.Ordinal);
        Assert.Contains("effective_to = case", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pos.fiscal_document_header_snapshots", source, StringComparison.Ordinal);
        Assert.DoesNotContain("delete from", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("print", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exit_authorization", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gate", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("apt", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NullableHeaderProfileListFiltersDeclareUuidParameterTypes()
    {
        var source = File.ReadAllText(FindProfileRepositorySourcePath());

        Assert.Contains("using NpgsqlTypes;", source, StringComparison.Ordinal);
        Assert.Contains("command.Parameters.Add(\"site_id\", NpgsqlDbType.Uuid)", source, StringComparison.Ordinal);
        Assert.Contains("command.Parameters.Add(\"site_pos_server_id\", NpgsqlDbType.Uuid)", source, StringComparison.Ordinal);
    }

    private static string FindRepositorySourcePath([CallerFilePath] string testFilePath = "")
    {
        var testSourceDirectory = Path.GetDirectoryName(testFilePath) ?? string.Empty;
        foreach (var startDirectory in new[] { testSourceDirectory, AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(startDirectory);
            while (current is not null)
            {
                var candidate = Path.Combine(current.FullName, "src", "ExitPass.PosServer.Persistence.Postgres", "FiscalDocuments", "PostgresFiscalDocumentRepository.cs");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }
        }

        throw new FileNotFoundException("Could not locate PostgresFiscalDocumentRepository.cs.");
    }

    private static string FindProfileRepositorySourcePath([CallerFilePath] string testFilePath = "")
    {
        var testSourceDirectory = Path.GetDirectoryName(testFilePath) ?? string.Empty;
        foreach (var startDirectory in new[] { testSourceDirectory, AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(startDirectory);
            while (current is not null)
            {
                var candidate = Path.Combine(current.FullName, "src", "ExitPass.PosServer.Persistence.Postgres", "FiscalDocuments", "PostgresSalesInvoiceHeaderProfileRepository.cs");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }
        }

        throw new FileNotFoundException("Could not locate PostgresSalesInvoiceHeaderProfileRepository.cs.");
    }

    private static string FiscalDocumentSemanticRequestHasherSource([CallerFilePath] string testFilePath = "")
    {
        var testSourceDirectory = Path.GetDirectoryName(testFilePath) ?? string.Empty;
        foreach (var startDirectory in new[] { testSourceDirectory, AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(startDirectory);
            while (current is not null)
            {
                var candidate = Path.Combine(current.FullName, "src", "ExitPass.PosServer.Runtime", "FiscalDocuments", "FiscalDocumentSemanticRequestHasher.cs");
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                current = current.Parent;
            }
        }

        throw new FileNotFoundException("Could not locate FiscalDocumentSemanticRequestHasher.cs.");
    }

    private static string FindTableSourcePath(string fileName, [CallerFilePath] string testFilePath = "")
    {
        var testSourceDirectory = Path.GetDirectoryName(testFilePath) ?? string.Empty;
        foreach (var startDirectory in new[] { testSourceDirectory, AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(startDirectory);
            while (current is not null)
            {
                var candidate = Path.Combine(current.FullName, "db", "state", "tables", fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }
        }

        throw new FileNotFoundException($"Could not locate {fileName}.");
    }

    private static string FindDatabaseScriptPath(string fileName, [CallerFilePath] string testFilePath = "")
    {
        var testSourceDirectory = Path.GetDirectoryName(testFilePath) ?? string.Empty;
        foreach (var startDirectory in new[] { testSourceDirectory, AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(startDirectory);
            while (current is not null)
            {
                var candidate = Path.Combine(current.FullName, "db", "scripts", fileName);
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
