using System.Runtime.CompilerServices;
using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests;

public sealed class PostgresFiscalDocumentReaderTests
{
    [Fact]
    public void ReaderSqlTargetsOnlyFiscalDocumentPersistenceTables()
    {
        var source = File.ReadAllText(FindReaderSourcePath());

        Assert.Contains("from pos.fiscal_documents", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("from pos.fiscal_document_status_history", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("from pos.fiscal_document_links", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("from pos.fiscal_document_lines", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("from pos.fiscal_tenders", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("from pos.fiscal_tax_details", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("from pos.fiscal_discount_privilege_details", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("from pos.fiscal_totals", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.fiscal_report", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.digital_si", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.annex", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.x_z", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exit_authorization", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gate_execution", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("open_gate", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReaderSqlIsReadOnlyAndParameterized()
    {
        var source = File.ReadAllText(FindReaderSourcePath());

        Assert.Contains("where fiscal_document_id = @fiscal_document_id", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("command.Parameters.AddWithValue(\"fiscal_document_id\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("insert into", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("update pos.", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("delete from", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("truncate", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BeginTransactionAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CommitAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ReaderSqlSelectsFiscalNumberingFieldsFromHeader()
    {
        var source = File.ReadAllText(FindReaderSourcePath());

        Assert.Contains("fiscal_identity_id", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_sequence_policy_id", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_sequence_value", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_document_number", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_series", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_number_prefix_text", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_number_suffix_text", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_number_assigned_at", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_number_assigned_by_ref", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GetNullableInt64(reader", source, StringComparison.Ordinal);
        Assert.Contains("GetNullableDateTimeOffset(reader", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ReaderOrdersChildRowsDeterministically()
    {
        var source = File.ReadAllText(FindReaderSourcePath());

        Assert.Contains("order by changed_at, fiscal_document_status_history_id", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("order by created_at, fiscal_document_link_id", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("order by line_sequence, fiscal_document_line_id", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("order by created_at, fiscal_tender_id", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("order by created_at, fiscal_tax_detail_id", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("order by created_at, fiscal_discount_privilege_detail_id", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("order by total_type_code_id, fiscal_total_id", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReaderFiltersSensitiveTextMarkers()
    {
        var source = File.ReadAllText(FindReaderSourcePath());

        Assert.Contains("SensitiveMarkers", source, StringComparison.Ordinal);
        Assert.Contains("raw_id", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("identity_document", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("evidence_payload", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("payment_payload", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("provider_callback", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("card_number", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("token", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secret", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ContainsSensitiveMarker(value) ? null : value", source, StringComparison.Ordinal);
    }

    private static string FindReaderSourcePath([CallerFilePath] string testFilePath = "")
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
                    "PostgresFiscalDocumentReader.cs");

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }
        }

        throw new FileNotFoundException("Could not locate PostgresFiscalDocumentReader.cs.");
    }
}
