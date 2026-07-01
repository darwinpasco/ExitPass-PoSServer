using ExitPass.PosServer.Runtime.FiscalDocuments;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests;

public sealed class FiscalDocumentReadServiceTests
{
    [Fact]
    public async Task ExistingDocumentIsReturnedFromReader()
    {
        var document = ValidReadModel();
        var service = new FiscalDocumentReadService(new StubFiscalDocumentReader(document));

        var result = await service.GetByIdAsync(document.FiscalDocumentId);

        Assert.True(result.Succeeded);
        Assert.Equal(FiscalDocumentReadErrorCode.None, result.ErrorCode);
        Assert.Same(document, result.Document);
    }

    [Fact]
    public async Task MissingDocumentReturnsDeterministicNotFound()
    {
        var documentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var service = new FiscalDocumentReadService(new StubFiscalDocumentReader(null));

        var result = await service.GetByIdAsync(documentId);

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentReadErrorCode.NotFound, result.ErrorCode);
        Assert.Null(result.Document);
        Assert.Contains(documentId.ToString(), result.Message, StringComparison.Ordinal);
    }

    private static FiscalDocumentReadModel ValidReadModel() =>
        new(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            null,
            null,
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "parking-session-001",
            "payment-attempt-001",
            "payment-confirmation-001",
            "central-finality-001",
            "vendor-ack-001",
            new DateOnly(2026, 7, 1),
            "{\"source_system\":\"central_pms\"}",
            true,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            [],
            [],
            [],
            [],
            [],
            [],
            []);

    private sealed class StubFiscalDocumentReader : IFiscalDocumentReader
    {
        private readonly FiscalDocumentReadModel? document;

        public StubFiscalDocumentReader(FiscalDocumentReadModel? document)
        {
            this.document = document;
        }

        public Task<FiscalDocumentReadModel?> GetByIdAsync(Guid fiscalDocumentId, CancellationToken cancellationToken) =>
            Task.FromResult(document?.FiscalDocumentId == fiscalDocumentId ? document : null);
    }
}
