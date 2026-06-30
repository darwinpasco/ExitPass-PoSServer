using ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests;

public sealed class PostgresFiscalDocumentRepositoryTests
{
    [Fact]
    public void InsertSqlTargetsOnlyFiscalDocumentHeaderStatusHistoryAndLinks()
    {
        var sql = PostgresFiscalDocumentSql.InsertFiscalDocument +
            PostgresFiscalDocumentSql.InsertFiscalDocumentStatusHistory +
            PostgresFiscalDocumentSql.InsertFiscalDocumentLink;

        Assert.Contains("insert into pos.fiscal_documents", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into pos.fiscal_document_status_history", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into pos.fiscal_document_links", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.fiscal_document_lines", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.fiscal_tenders", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.fiscal_tax_details", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.fiscal_discount_privilege_details", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.fiscal_totals", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.fiscal_report", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.digital_si", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.audit", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exit", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gate", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InsertSqlUsesParameterizedValues()
    {
        var sql = PostgresFiscalDocumentSql.InsertFiscalDocument +
            PostgresFiscalDocumentSql.InsertFiscalDocumentStatusHistory +
            PostgresFiscalDocumentSql.InsertFiscalDocumentLink;
        var untrusted = "payable-basis-001'); drop table pos.fiscal_documents; --";

        Assert.Contains("@fiscal_document_id", sql, StringComparison.Ordinal);
        Assert.Contains("@site_pos_server_id", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_document_status_history_id", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_document_status_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@source_fiscal_document_id", sql, StringComparison.Ordinal);
        Assert.Contains("@target_fiscal_document_id", sql, StringComparison.Ordinal);
        Assert.Contains("@fiscal_document_link_type_code_id", sql, StringComparison.Ordinal);
        Assert.Contains("@document_context", sql, StringComparison.Ordinal);
        Assert.DoesNotContain(untrusted, sql, StringComparison.Ordinal);
        Assert.DoesNotContain("drop table", sql, StringComparison.OrdinalIgnoreCase);
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
    public void RepositoryUsesSingleTransactionForHeaderStatusHistoryAndLinks()
    {
        var repositorySource = File.ReadAllText(FindRepositorySourcePath());

        Assert.Contains("BeginTransactionAsync", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertFiscalDocument,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertFiscalDocumentStatusHistory,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertFiscalDocumentLink,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("CommitAsync", repositorySource, StringComparison.Ordinal);
        Assert.Contains("RollbackAsync", repositorySource, StringComparison.Ordinal);

        var headerIndex = repositorySource.IndexOf("InsertFiscalDocument,", StringComparison.Ordinal);
        var statusIndex = repositorySource.IndexOf("InsertFiscalDocumentStatusHistory,", StringComparison.Ordinal);
        var linkIndex = repositorySource.IndexOf("InsertFiscalDocumentLink,", StringComparison.Ordinal);
        var commitIndex = repositorySource.IndexOf("CommitAsync", StringComparison.Ordinal);
        Assert.True(headerIndex < statusIndex);
        Assert.True(statusIndex < linkIndex);
        Assert.True(linkIndex < commitIndex);
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
        Assert.DoesNotContain("raw_id", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("evidence_payload", json, StringComparison.OrdinalIgnoreCase);
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
            "FiscalDocumentLine",
            "FiscalTender",
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
            null,
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            "site-pos-server-001",
            "sales_invoice",
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
            [new FiscalDiscountReferenceInput("discount-validation-001", FiscalDiscountReferenceStatus.Approved, true)]);

    private static string FindRepositorySourcePath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
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

        throw new FileNotFoundException("Could not locate PostgresFiscalDocumentRepository.cs.");
    }
}
