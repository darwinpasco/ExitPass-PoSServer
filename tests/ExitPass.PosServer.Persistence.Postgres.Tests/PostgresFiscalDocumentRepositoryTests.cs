using ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using System.Runtime.CompilerServices;
using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests;

public sealed class PostgresFiscalDocumentRepositoryTests
{
    [Fact]
    public void InsertSqlTargetsOnlyFiscalDocumentHeaderStatusHistoryLinksAndLines()
    {
        var sql = PostgresFiscalDocumentSql.InsertFiscalDocument +
            PostgresFiscalDocumentSql.InsertFiscalDocumentStatusHistory +
            PostgresFiscalDocumentSql.InsertFiscalDocumentLink +
            PostgresFiscalDocumentSql.InsertFiscalDocumentLine;

        Assert.Contains("insert into pos.fiscal_documents", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into pos.fiscal_document_status_history", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into pos.fiscal_document_links", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into pos.fiscal_document_lines", sql, StringComparison.OrdinalIgnoreCase);
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
            PostgresFiscalDocumentSql.InsertFiscalDocumentLink +
            PostgresFiscalDocumentSql.InsertFiscalDocumentLine;
        var untrusted = "payable-basis-001'); drop table pos.fiscal_documents; --";

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
    public void RepositoryUsesSingleTransactionForHeaderStatusHistoryLinksAndLines()
    {
        var repositorySource = File.ReadAllText(FindRepositorySourcePath());

        Assert.Contains("BeginTransactionAsync", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertFiscalDocument,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertFiscalDocumentStatusHistory,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertFiscalDocumentLink,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalDocumentSql.InsertFiscalDocumentLine,", repositorySource, StringComparison.Ordinal);
        Assert.Contains("CommitAsync", repositorySource, StringComparison.Ordinal);
        Assert.Contains("RollbackAsync", repositorySource, StringComparison.Ordinal);

        var headerIndex = repositorySource.IndexOf("InsertFiscalDocument,", StringComparison.Ordinal);
        var statusIndex = repositorySource.IndexOf("InsertFiscalDocumentStatusHistory,", StringComparison.Ordinal);
        var linkIndex = repositorySource.IndexOf("InsertFiscalDocumentLink,", StringComparison.Ordinal);
        var lineIndex = repositorySource.IndexOf("InsertFiscalDocumentLine,", StringComparison.Ordinal);
        var commitIndex = repositorySource.IndexOf("CommitAsync", StringComparison.Ordinal);
        Assert.True(headerIndex < statusIndex);
        Assert.True(statusIndex < linkIndex);
        Assert.True(linkIndex < lineIndex);
        Assert.True(lineIndex < commitIndex);
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
        Assert.DoesNotContain("raw_id", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("evidence_payload", json, StringComparison.OrdinalIgnoreCase);
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
            [new FiscalDiscountReferenceInput("discount-validation-001", FiscalDiscountReferenceStatus.Approved, true)]);

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
}
