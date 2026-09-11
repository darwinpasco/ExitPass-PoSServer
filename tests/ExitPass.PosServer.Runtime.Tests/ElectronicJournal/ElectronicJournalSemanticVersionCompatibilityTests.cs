using ExitPass.PosServer.Runtime.ElectronicJournal;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.ElectronicJournal;

public sealed class ElectronicJournalSemanticVersionCompatibilityTests
{
    private const string StreamId = "4138A9FE3E16422E8518172174467BB5";
    private static readonly Guid SiteId = Guid.Parse("3a138565-1b88-55f8-c83d-5380db6edccc");
    private static readonly Guid IdentityId = Guid.Parse("ad02beb5-b8cd-4545-9ff2-5586782c686a");
    private static readonly Guid SequencePolicyId = Guid.Parse("2e4252dd-38f0-5424-bee2-0c864b41f6b2");

    [Fact]
    public void LegacyV1CanonicalTextExactlyMatchesTheHistoricalProfile()
    {
        var request = Request(
            Guid.Parse("53474326-5752-4734-882a-370f5843c19c"),
            Guid.Parse("89ecc51f-ec76-5867-a0cd-b7b28e926543"),
            new DateOnly(2026, 9, 7),
            "2026-09-07T06:43:33.845746Z",
            "PAYMENT_CONFIRMATION:7174304a-a81f-41e4-b08c-b2a140ab232f",
            1, 2500, string.Empty) with
        {
            PrintableSalesInvoiceText = "historical text must not enter v1"
        };

        const string expected =
            "profile=pos-server-electronic-journal-event-semantic:sha256:v1\n" +
            "event_schema=pos-server-electronic-journal-event:v1\n" +
            "site_pos_server_id=3a138565-1b88-55f8-c83d-5380db6edccc\n" +
            "fiscal_identity_id=ad02beb5-b8cd-4545-9ff2-5586782c686a\n" +
            "currency=PHP\n" +
            "fiscal_reporting_period_id=89ecc51f-ec76-5867-a0cd-b7b28e926543\n" +
            "event_type=fiscal_document_committed\n" +
            "source_transition_ref=fiscal-document:53474326-5752-4734-882a-370f5843c19c\n" +
            "source_transition_version=sha256:v1\n" +
            "effective_at=2026-09-07T06:43:33.8457460Z\n" +
            "fiscal_document_id=53474326-5752-4734-882a-370f5843c19c\n" +
            "fiscal_report_request_id=\n" +
            "x_z_report_id=\n" +
            "bir_sales_summary_report_id=\n" +
            "reprint_request_id=\n" +
            "fiscal_sequence_policy_id=2e4252dd-38f0-5424-bee2-0c864b41f6b2\n" +
            "business_day_date=2026-09-07\n" +
            "idempotency_ref=PAYMENT_CONFIRMATION:7174304a-a81f-41e4-b08c-b2a140ab232f\n" +
            "facts={\"discount_detail_count\":\"0\",\"discount_facts\":\"\",\"fiscal_document_number\":\"SI-00000001\",\"fiscal_document_type\":\"sales_invoice\",\"fiscal_sequence_value\":\"1\",\"fiscal_series\":\"PITX-L3-SI\",\"line_count\":\"1\",\"payable_amount_minor_units\":\"2500\",\"tax_detail_count\":\"0\",\"tax_facts\":\"\",\"tender_count\":\"1\",\"tender_facts\":\"a4bd3153-076e-564f-af56-3532be501e95:2500\",\"total_count\":\"1\",\"total_facts\":\"6abfc6aa-b94e-5a24-b533-8d18a33c468b:2500\"}\n";

        var actual = ElectronicJournalCanonicalizer.CanonicalSemanticText(
            request, ElectronicJournalContract.LegacySemanticHashVersion);

        Assert.Equal(expected, actual);
        Assert.DoesNotContain("printable_sales_invoice_text_sha256", actual, StringComparison.Ordinal);
    }

    [Fact]
    public void CurrentV2AlwaysIncludesPrintableTextDigestAndDiffersFromV1()
    {
        var request = HistoricalVectors()[0].Request;
        var withoutText = ElectronicJournalCanonicalizer.CanonicalSemanticText(
            request, ElectronicJournalContract.CurrentSemanticHashVersion);
        var withTextRequest = request with { PrintableSalesInvoiceText = "SALES INVOICE\r\nExact text\r\n" };
        var withText = ElectronicJournalCanonicalizer.CanonicalSemanticText(
            withTextRequest, ElectronicJournalContract.CurrentSemanticHashVersion);

        Assert.Contains("printable_sales_invoice_text_sha256=\n", withoutText, StringComparison.Ordinal);
        Assert.Contains(
            $"printable_sales_invoice_text_sha256={ElectronicJournalCanonicalizer.Sha256(withTextRequest.PrintableSalesInvoiceText!)}\n",
            withText, StringComparison.Ordinal);
        Assert.NotEqual(
            ElectronicJournalCanonicalizer.ComputeSemanticHash(request, ElectronicJournalContract.LegacySemanticHashVersion),
            ElectronicJournalCanonicalizer.ComputeSemanticHash(request, ElectronicJournalContract.CurrentSemanticHashVersion));
    }

    [Fact]
    public void UnknownSemanticVersionFailsClosed()
    {
        var exception = Assert.Throws<UnsupportedElectronicJournalSemanticHashVersionException>(() =>
            ElectronicJournalCanonicalizer.ComputeSemanticHash(
                HistoricalVectors()[0].Request,
                "pos-server-electronic-journal-event-semantic:sha256:v999"));

        Assert.Contains("v999", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AllTenPersistentHistoricalV1SemanticAndIntegrityHashesRecomputeExactly()
    {
        var vectors = HistoricalVectors();
        Assert.Equal(10, vectors.Count);

        var previous = ElectronicJournalContract.GenesisHash;
        foreach (var vector in vectors)
        {
            Assert.Null(vector.Request.PrintableSalesInvoiceText);
            Assert.Equal(previous, vector.PreviousIntegrityHash);
            Assert.Equal(vector.SemanticHash, ElectronicJournalCanonicalizer.ComputeSemanticHash(
                vector.Request, ElectronicJournalContract.LegacySemanticHashVersion));
            Assert.Equal(vector.IntegrityHash, ElectronicJournalCanonicalizer.ComputeIntegrityHash(
                vector.Request, vector.EventReference, vector.Sequence, vector.RecordedAt,
                vector.SemanticHash, vector.PreviousIntegrityHash));
            previous = vector.IntegrityHash;
        }

        Assert.Equal("76a4c1b74edd672f6a043e6f1f167e75e3e20a29de2cca546935edbf616ea838", previous);
    }

    [Fact]
    public void MixedTenV1AndOneV2ChainVerifiesWithV2Head()
    {
        var historical = HistoricalVectors();
        var request = Request(
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            Guid.Parse("1ee930c2-a471-5b34-8329-69974a64142c"),
            new DateOnly(2026, 9, 8), "2026-09-08T01:00:00Z", "new-v2-operation", 11, 5000,
            "ab180f41-e181-5579-b9f1-5ae7a840a946:4464:536") with
        {
            PrintableSalesInvoiceText = "SALES INVOICE\r\nV2 exact text\r\n"
        };
        var semantic = ElectronicJournalCanonicalizer.ComputeSemanticHash(
            request, ElectronicJournalContract.CurrentSemanticHashVersion);
        var recordedAt = DateTimeOffset.Parse("2026-09-08T01:00:01Z");
        var previous = historical[^1].IntegrityHash;
        var head = ElectronicJournalCanonicalizer.ComputeIntegrityHash(
            request, $"EJ-{StreamId}-00000000000000000011", 11, recordedAt, semantic, previous);

        Assert.Equal(10, historical.Count(vector =>
            ElectronicJournalCanonicalizer.ComputeSemanticHash(
                vector.Request, ElectronicJournalContract.LegacySemanticHashVersion) == vector.SemanticHash));
        Assert.Equal(semantic, ElectronicJournalCanonicalizer.ComputeSemanticHash(
            request, ElectronicJournalContract.CurrentSemanticHashVersion));
        Assert.NotEqual(previous, head);
        Assert.Matches("^[0-9a-f]{64}$", head);
    }

    [Fact]
    public void ReportOnlyEventWithNullPrintableTextStillUsesV2EmptyDigestLine()
    {
        var request = HistoricalVectors()[0].Request with
        {
            EventType = "x_reading_committed",
            FiscalDocumentId = null,
            FiscalReportRequestId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
            XZReportId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
            FiscalSequencePolicyId = null,
            SourceTransitionReference = "fiscal-report-request:bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
            PrintableSalesInvoiceText = null
        };

        var canonical = ElectronicJournalCanonicalizer.CanonicalSemanticText(
            request, ElectronicJournalContract.CurrentSemanticHashVersion);

        Assert.StartsWith($"profile={ElectronicJournalContract.CurrentSemanticHashVersion}\n", canonical, StringComparison.Ordinal);
        Assert.Contains("printable_sales_invoice_text_sha256=\n", canonical, StringComparison.Ordinal);
    }

    private static IReadOnlyList<HistoricalVector> HistoricalVectors()
    {
        var legacyPeriod = Guid.Parse("89ecc51f-ec76-5867-a0cd-b7b28e926543");
        var currentPeriod = Guid.Parse("1ee930c2-a471-5b34-8329-69974a64142c");
        return
        [
            Vector(1, "53474326-5752-4734-882a-370f5843c19c", legacyPeriod, "2026-09-07", "2026-09-07T06:43:33.845746Z", "2026-09-07T06:43:34.010416Z", "7174304a-a81f-41e4-b08c-b2a140ab232f", 2500, "", "142be3d4d8f56c5e86c18480cbf70904b9d894261051805d35fbef9f9d293099", ElectronicJournalContract.GenesisHash, "e1e1f5c010249d7522467d7b369b4f5d58b546f4a98f55aa0d72e47dc4e525ff"),
            Vector(2, "2e5c381f-8d12-4a41-a0ca-f4daae32146a", legacyPeriod, "2026-09-07", "2026-09-07T06:50:26.848919Z", "2026-09-07T06:50:26.873075Z", "8bc9b6c9-0b84-41d3-9ed7-f15cbb163daa", 3000, "", "656fd08c0167266a158356f5da52f850baf17c7256f248b11ad2646a389beca2", "e1e1f5c010249d7522467d7b369b4f5d58b546f4a98f55aa0d72e47dc4e525ff", "8329b2b82d03a981d143b59db5edc22a3db63b63d52f971bb6aadecb58d7daf2"),
            Vector(3, "1ce48d30-6dce-445d-9eb2-dff00e6015bd", legacyPeriod, "2026-09-07", "2026-09-07T09:13:03.374152Z", "2026-09-07T09:13:03.454849Z", "067219ab-95d2-497c-9f11-3653905a8b6a", 4000, "", "66b7b6383c30ac64d5436d8d2c294aa744d8c9524069bf082aa9e710f59ddd23", "8329b2b82d03a981d143b59db5edc22a3db63b63d52f971bb6aadecb58d7daf2", "cf687c4655ee8c2b08ec359ff91d5e80ad4c9c2a241299dcba44eb42c8987a72"),
            Vector(4, "52b608b4-8470-410f-8c63-426692753837", currentPeriod, "2026-09-08", "2026-09-07T16:50:57.648869Z", "2026-09-07T16:50:57.752099Z", "f78e6a1d-936b-4fd5-8ac3-c513fe2134ff", 2500, "", "ac558ad937eadc3b40a8db50d354991374ca3a84fdfa365312df6faf1e332f5f", "cf687c4655ee8c2b08ec359ff91d5e80ad4c9c2a241299dcba44eb42c8987a72", "0a3879840e9d0789d17f999cc5a95761ff9a0724843002378a53495e7aa3fab2"),
            Vector(5, "41f7e2dc-fda9-4275-97d3-00bd790eec61", currentPeriod, "2026-09-08", "2026-09-07T17:14:51.074975Z", "2026-09-07T17:14:51.114112Z", "3d38e4fd-edb5-4540-946a-8c5c90a64d35", 3000, "", "a0679f78366b2afec0f6a12561d9b6058b8328b38fabc436d654f9f492bb3385", "0a3879840e9d0789d17f999cc5a95761ff9a0724843002378a53495e7aa3fab2", "7ecc11f17cc15fd68b4d6632b1d03a4b5707b87f716ef449d7de8ed89ab369ff"),
            Vector(6, "a2d1b101-45ed-45aa-bbc3-3afa35fa4f6e", currentPeriod, "2026-09-08", "2026-09-07T21:18:27.822856Z", "2026-09-07T21:18:28.002812Z", "d286d99d-e881-4eca-8b8f-25070cb890a3", 3000, "ab180f41-e181-5579-b9f1-5ae7a840a946:2679:321", "78760b6503313dd860fd67a7a490e46fe992cc5b587f1e6644c35dac3764fa0e", "7ecc11f17cc15fd68b4d6632b1d03a4b5707b87f716ef449d7de8ed89ab369ff", "931adee06154a97ebbc2b2bb944eda003562600e58db0a50051d124548260275"),
            Vector(7, "54622b63-77f8-43f1-b033-0163c689ce2a", currentPeriod, "2026-09-08", "2026-09-07T23:16:24.193017Z", "2026-09-07T23:16:24.244809Z", "821de16e-47ae-454f-918f-cf7b4c2e89bd", 3000, "ab180f41-e181-5579-b9f1-5ae7a840a946:2679:321", "6ee841b3bc24d7aa297fde42742dfc1ed1dab3379bd2968d51af2d719ea37b76", "931adee06154a97ebbc2b2bb944eda003562600e58db0a50051d124548260275", "25502f38bb708a6f91f3bb15c12ddc47ef08c1653ff1898a4e65335122504d58"),
            Vector(8, "5659b0cf-6be0-4c59-af73-1354259c1a90", currentPeriod, "2026-09-08", "2026-09-07T23:34:29.172987Z", "2026-09-07T23:34:29.205906Z", "5e4f1ddb-6a1b-497d-99db-1d38b339f088", 2500, "ab180f41-e181-5579-b9f1-5ae7a840a946:2232:268", "65bb9a65c8b3e375a4cfb0ef3a36fdbc7741779212bec8e4c367ec6e958641fe", "25502f38bb708a6f91f3bb15c12ddc47ef08c1653ff1898a4e65335122504d58", "68c4d315366345c56bc99fe271b66482e4f421e67f69b928b950ba7f5fc45a3e"),
            Vector(9, "30f577c6-cde0-400e-9f37-bba069d2c38f", currentPeriod, "2026-09-08", "2026-09-07T23:53:08.909548Z", "2026-09-07T23:53:08.93714Z", "2de4fe5f-aeec-4fa1-825b-eda4331a6065", 4500, "ab180f41-e181-5579-b9f1-5ae7a840a946:4018:482", "2bd494b0b0ad6145242ef8c9d4cff302541e53b7e66d2b38cf0185768c7c593f", "68c4d315366345c56bc99fe271b66482e4f421e67f69b928b950ba7f5fc45a3e", "38466fa5ec8cea130855b2e89a94726e477e2587ee8d3c2a1113452eaf619287"),
            Vector(10, "7fb74743-2360-434f-aa45-6b39534e9782", currentPeriod, "2026-09-08", "2026-09-08T00:53:55.129413Z", "2026-09-08T00:53:55.176382Z", "b946baec-7105-40cb-b381-e1cac1844f7c", 2500, "ab180f41-e181-5579-b9f1-5ae7a840a946:2232:268", "6445d5c6cee125db1f9410f824796b10d75913a8ef18a671e79afaea0820d184", "38466fa5ec8cea130855b2e89a94726e477e2587ee8d3c2a1113452eaf619287", "76a4c1b74edd672f6a043e6f1f167e75e3e20a29de2cca546935edbf616ea838")
        ];
    }

    private static HistoricalVector Vector(
        long sequence, string documentId, Guid periodId, string businessDate, string effectiveAt,
        string recordedAt, string paymentConfirmationId, long amount, string taxFacts,
        string semanticHash, string previousIntegrityHash, string integrityHash)
    {
        var correlation = $"PAYMENT_CONFIRMATION:{paymentConfirmationId}";
        var request = Request(Guid.Parse(documentId), periodId, DateOnly.Parse(businessDate), effectiveAt,
            correlation, sequence, amount, taxFacts);
        return new(
            sequence,
            $"EJ-{StreamId}-{sequence:D20}",
            request,
            DateTimeOffset.Parse(recordedAt),
            semanticHash,
            previousIntegrityHash,
            integrityHash);
    }

    private static ElectronicJournalAppendRequest Request(
        Guid documentId, Guid periodId, DateOnly businessDate, string effectiveAt,
        string correlation, long sequence, long amount, string taxFacts) => new(
        SiteId,
        IdentityId,
        "PHP",
        periodId,
        "fiscal_document_committed",
        $"fiscal-document:{documentId:D}",
        "sha256:v1",
        DateTimeOffset.Parse(effectiveAt),
        "pos-server-fiscal-document-runtime",
        "pos-server-fiscal-document-runtime",
        correlation,
        new SortedDictionary<string, string?>
        {
            ["tax_facts"] = taxFacts,
            ["line_count"] = "1",
            ["total_count"] = "1",
            ["total_facts"] = $"6abfc6aa-b94e-5a24-b533-8d18a33c468b:{amount}",
            ["tender_count"] = "1",
            ["tender_facts"] = $"a4bd3153-076e-564f-af56-3532be501e95:{amount}",
            ["fiscal_series"] = "PITX-L3-SI",
            ["discount_facts"] = string.Empty,
            ["tax_detail_count"] = string.IsNullOrEmpty(taxFacts) ? "0" : "1",
            ["fiscal_document_type"] = "sales_invoice",
            ["discount_detail_count"] = "0",
            ["fiscal_sequence_value"] = sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["fiscal_document_number"] = $"SI-{sequence:D8}",
            ["payable_amount_minor_units"] = amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
        },
        documentId,
        FiscalSequencePolicyId: SequencePolicyId,
        BusinessDayDate: businessDate,
        IdempotencyReference: correlation);

    private sealed record HistoricalVector(
        long Sequence,
        string EventReference,
        ElectronicJournalAppendRequest Request,
        DateTimeOffset RecordedAt,
        string SemanticHash,
        string PreviousIntegrityHash,
        string IntegrityHash);
}
