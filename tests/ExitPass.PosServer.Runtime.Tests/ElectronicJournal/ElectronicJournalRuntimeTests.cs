using System.Text;
using ExitPass.PosServer.Runtime.ElectronicJournal;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.ElectronicJournal;

public sealed class ElectronicJournalRuntimeTests
{
    private static readonly Guid Site = Guid.Parse("81000000-0000-4000-8000-000000000001");
    private static readonly Guid Identity = Guid.Parse("81000000-0000-4000-8000-000000000002");
    private static readonly Guid Period = Guid.Parse("81000000-0000-4000-8000-000000000003");

    [Fact]
    public void SemanticHashIsCanonicalAndChangedFactsConflict()
    {
        var first = Request(new Dictionary<string, string?> { ["z"] = "2", ["a"] = "1" });
        var reordered = Request(new Dictionary<string, string?> { ["a"] = "1", ["z"] = "2" });
        var changed = Request(new Dictionary<string, string?> { ["a"] = "1", ["z"] = "3" });

        Assert.Equal(ElectronicJournalCanonicalizer.ComputeSemanticHash(first), ElectronicJournalCanonicalizer.ComputeSemanticHash(reordered));
        Assert.NotEqual(ElectronicJournalCanonicalizer.ComputeSemanticHash(first), ElectronicJournalCanonicalizer.ComputeSemanticHash(changed));
        Assert.Equal("{\"a\":\"1\",\"z\":\"2\"}", ElectronicJournalCanonicalizer.CanonicalFactsJson(first.Facts));
    }

    [Fact]
    public void IntegrityHashBindsChronologyAndAttribution()
    {
        var request = Request(new Dictionary<string, string?> { ["amount_minor_units"] = "100" });
        var semantic = ElectronicJournalCanonicalizer.ComputeSemanticHash(request);
        var recorded = DateTimeOffset.Parse("2026-08-10T01:02:03Z");
        var hash = ElectronicJournalCanonicalizer.ComputeIntegrityHash(request, "EJ-1", 1, recorded, semantic, ElectronicJournalContract.GenesisHash);
        var changedActor = ElectronicJournalCanonicalizer.ComputeIntegrityHash(request with { ActorReference = "different" }, "EJ-1", 1, recorded, semantic, ElectronicJournalContract.GenesisHash);
        var reordered = ElectronicJournalCanonicalizer.ComputeIntegrityHash(request, "EJ-1", 2, recorded, semantic, ElectronicJournalContract.GenesisHash);

        Assert.NotEqual(hash, changedActor);
        Assert.NotEqual(hash, reordered);
    }

    [Fact]
    public void StorageLocationDoesNotEnterSemanticOrIntegrityCanonicalText()
    {
        var request = Request(new Dictionary<string, string?> { ["fiscal_document_number"] = "SI-0001" }) with
        {
            EventType = "fiscal_document_committed",
            FiscalDocumentId = Guid.Parse("81000000-0000-4000-8000-000000000004"),
            PrintableSalesInvoiceText = "SALES INVOICE\r\nExact Ñ UTF-8 text\r\n"
        };
        var canonical = ElectronicJournalCanonicalizer.CanonicalSemanticText(request);
        var semantic = ElectronicJournalCanonicalizer.ComputeSemanticHash(request);
        var recorded = DateTimeOffset.Parse("2026-08-10T01:02:03Z");
        var integrity = ElectronicJournalCanonicalizer.ComputeIntegrityHash(
            request, "EJ-1", 1, recorded, semantic, ElectronicJournalContract.GenesisHash);

        Assert.Contains($"printable_sales_invoice_text_sha256={ElectronicJournalCanonicalizer.Sha256(request.PrintableSalesInvoiceText!)}", canonical, StringComparison.Ordinal);
        Assert.DoesNotContain("journal_context", canonical, StringComparison.Ordinal);
        Assert.DoesNotContain("printable_sales_invoice_text=", canonical, StringComparison.Ordinal);
        Assert.Equal(semantic, ElectronicJournalCanonicalizer.ComputeSemanticHash(request));
        Assert.Equal(integrity, ElectronicJournalCanonicalizer.ComputeIntegrityHash(
            request, "EJ-1", 1, recorded, semantic, ElectronicJournalContract.GenesisHash));
        Assert.Equal("pos-server-electronic-journal-event-semantic:sha256:v1", ElectronicJournalContract.SemanticHashVersion);
    }

    [Fact]
    public void JsonAndCsvExportsAreByteIdenticalAcrossRendererInstances()
    {
        var page = new ElectronicJournalPage([Event()], null, 1, ElectronicJournalContract.ChronologyVersion);
        var query = new ElectronicJournalQuery(Site, Identity, "PHP");
        var first = new ElectronicJournalExportRenderer();
        var restarted = new ElectronicJournalExportRenderer();

        foreach (var format in Enum.GetValues<ElectronicJournalExportFormat>())
        {
            var one = first.Render(page, query, format);
            var two = restarted.Render(page, query, format);
            Assert.Equal(one.Bytes, two.Bytes);
            Assert.Equal(one.ContentSha256, two.ContentSha256);
            Assert.Equal(one.OutputIdentity, two.OutputIdentity);
            var text = Encoding.UTF8.GetString(one.Bytes);
            Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("statutory_id", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("stack", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ExportIdentityBindsFilterSemantics()
    {
        var page = new ElectronicJournalPage([Event()], null, 1, ElectronicJournalContract.ChronologyVersion);
        var renderer = new ElectronicJournalExportRenderer();
        var all = renderer.Render(page, new ElectronicJournalQuery(Site, Identity, "PHP"), ElectronicJournalExportFormat.Json);
        var filtered = renderer.Render(page,
            new ElectronicJournalQuery(Site, Identity, "PHP", EventType: "x_reading_committed"),
            ElectronicJournalExportFormat.Json);

        Assert.NotEqual(all.OutputIdentity, filtered.OutputIdentity);
        Assert.NotEqual(all.FileName, filtered.FileName);
    }

    private static ElectronicJournalAppendRequest Request(IReadOnlyDictionary<string, string?> facts) => new(
        Site, Identity, "PHP", Period, "x_reading_committed", "source:1", "v1",
        DateTimeOffset.Parse("2026-08-10T01:00:00Z"), "actor", "service", "correlation", facts,
        BusinessDayDate: new DateOnly(2026, 8, 10), IdempotencyReference: "operation");

    private static ElectronicJournalEvent Event()
    {
        var request = Request(new SortedDictionary<string, string?> { ["amount_minor_units"] = "100" });
        var semantic = ElectronicJournalCanonicalizer.ComputeSemanticHash(request);
        var recorded = DateTimeOffset.Parse("2026-08-10T01:00:01Z");
        var hash = ElectronicJournalCanonicalizer.ComputeIntegrityHash(request, "EJ-1", 1, recorded, semantic, ElectronicJournalContract.GenesisHash);
        return new("EJ-1", request.EventType, ElectronicJournalContract.EventSchemaVersion, Site, Identity, "PHP", Period,
            null, null, null, null, null, request.BusinessDayDate, 1, request.EffectiveAt, recorded, request.ActorReference,
            request.ServiceIdentityReference, request.CorrelationReference, request.SourceTransitionReference,
            request.SourceTransitionVersion, request.IdempotencyReference, ElectronicJournalContract.SemanticHashVersion,
            semantic, ElectronicJournalContract.IntegrityHashVersion, ElectronicJournalContract.GenesisHash, hash,
            "fiscal_reconstruction_hold", request.Facts);
    }
}
