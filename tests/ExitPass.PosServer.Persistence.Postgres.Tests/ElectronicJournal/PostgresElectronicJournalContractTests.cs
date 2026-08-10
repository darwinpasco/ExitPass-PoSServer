using ExitPass.PosServer.Persistence.Postgres.ElectronicJournal;
using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests.ElectronicJournal;

public sealed class PostgresElectronicJournalContractTests
{
    [Fact]
    public void WriterUsesStreamRowLockExactlyOnceIdentityAndAtomicTransactionInputs()
    {
        var source = File.ReadAllText(Find("PostgresElectronicJournalWriter.cs"));
        Assert.Contains("FOR UPDATE", source, StringComparison.Ordinal);
        Assert.Contains("ux_ej_records__source_transition", File.ReadAllText(FindSchema("pos.electronic_journal_records.sql")), StringComparison.Ordinal);
        Assert.Contains("NpgsqlTransaction transaction", source, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DELETE FROM pos.electronic_journal", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE pos.electronic_journal_records", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanonicalEventsAreImmutableFamilyEnforcedAndLegacyRowsRemainNonCanonical()
    {
        var source = File.ReadAllText(FindSchema("pos.electronic_journal_records.sql"));
        Assert.Contains("trg_electronic_journal_records_immutable", source, StringComparison.Ordinal);
        Assert.Contains("ck_ej_records__event_type_family", source, StringComparison.Ordinal);
        Assert.Contains("is_canonical boolean NOT NULL DEFAULT false", source, StringComparison.Ordinal);
        Assert.Contains("journal_context IS NULL", source, StringComparison.Ordinal);
    }

    [Fact]
    public void WriterRejectsSensitiveAndUnboundedFactMaterial()
    {
        var source = File.ReadAllText(Find("PostgresElectronicJournalWriter.cs"));
        Assert.Contains("ProhibitedFactKeyFragments", source, StringComparison.Ordinal);
        Assert.Contains("statutory_id", source, StringComparison.Ordinal);
        Assert.Contains("credential", source, StringComparison.Ordinal);
        Assert.Contains("Length: > 2048", source, StringComparison.Ordinal);
        Assert.Contains("Canonical Electronic Journal event facts violate the privacy-safe contract", source, StringComparison.Ordinal);
    }

    [Fact]
    public void EverySupportedAuthoritativeRepositoryAppendsBeforeCommit()
    {
        foreach (var relative in new[]
        {
            "FiscalDocuments/PostgresFiscalDocumentRepository.cs",
            "FiscalDocuments/PostgresFiscalDocumentReprintRepository.cs",
            "FiscalReports/PostgresFiscalXReadingRepository.cs",
            "FiscalReports/PostgresFiscalZReadingRepository.cs",
            "FiscalReports/PostgresBirSalesSummaryRepository.cs"
        })
        {
            var source = File.ReadAllText(FindRelative(relative));
            Assert.Contains("PostgresElectronicJournalWriter.AppendAsync", source, StringComparison.Ordinal);
            Assert.True(source.IndexOf("PostgresElectronicJournalWriter.AppendAsync", StringComparison.Ordinal) <
                        source.LastIndexOf("CommitAsync", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void CanonicalReprintHasBidirectionalDatabaseBindingAndImmutableCopySequence()
    {
        var requests = File.ReadAllText(FindSchema("pos.reprint_requests.sql"));
        var outputs = File.ReadAllText(FindSchema("pos.reprint_output_refs.sql"));
        var events = File.ReadAllText(FindSchema("pos.electronic_journal_records.sql"));
        Assert.Contains("DEFERRABLE INITIALLY DEFERRED", requests, StringComparison.Ordinal);
        Assert.Contains("require_canonical_reprint_journal_event", requests, StringComparison.Ordinal);
        Assert.Contains("ux_reprint_requests__document_copy", requests, StringComparison.Ordinal);
        Assert.Contains("trg_reprint_requests_immutable", requests, StringComparison.Ordinal);
        Assert.Contains("trg_reprint_output_refs_immutable", outputs, StringComparison.Ordinal);
        Assert.Contains("fk_ej_records__reprint", events, StringComparison.Ordinal);
        Assert.Contains("reprint_request_id IS NOT NULL", events, StringComparison.Ordinal);
    }

    private static string Find(string name) => FindRelative($"ElectronicJournal/{name}");
    private static string FindRelative(string relative) => FindRoot("src", "ExitPass.PosServer.Persistence.Postgres", relative.Replace('/', Path.DirectorySeparatorChar));
    private static string FindSchema(string name) => FindRoot("db", "state", "tables", name);
    private static string FindRoot(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. parts]);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException(parts[^1]);
    }
}
