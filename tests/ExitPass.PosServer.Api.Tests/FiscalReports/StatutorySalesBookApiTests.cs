using System.IO.Compression;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace ExitPass.PosServer.Api.Tests.FiscalReports;

public sealed class StatutorySalesBookApiTests
{
    [Fact]
    public async Task ReadAndExportUseNarrowIndependentPermissions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPosServerFiscalDocumentApi(new ConfigurationBuilder().AddInMemoryCollection().Build());
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(Principal(StatutorySalesBookAuthorization.ReadPermission), null,
            options.GetPolicy(StatutorySalesBookAuthorization.ReadPolicyName)!)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal(StatutorySalesBookAuthorization.ReadPermission), null,
            options.GetPolicy(StatutorySalesBookAuthorization.ExportPolicyName)!)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal(StatutorySalesBookAuthorization.ExportPermission), null,
            options.GetPolicy(StatutorySalesBookAuthorization.ExportPolicyName)!)).Succeeded);
    }

    [Theory]
    [InlineData("e2", StatutorySalesBookType.AnnexE2)]
    [InlineData("e3", StatutorySalesBookType.AnnexE3)]
    public async Task ReadUsesExactPosServerIdentityAndMonthScope(string annex, StatutorySalesBookType type)
    {
        var repository = new StubRepository(type);
        var response = await Execute(context => StatutorySalesBookEndpoint.ReadAsync(
            annex, Request(), new(repository), repository, context, new Host(Environments.Production), default));

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(type, repository.Query!.Type);
        Assert.Equal(Site, repository.Query.SitePosServerId);
        Assert.Equal(Identity, repository.Query.FiscalIdentityId);
        Assert.Equal(2026, repository.Query.CalendarYear);
        Assert.Equal(10, repository.Query.CalendarMonth);
        Assert.Equal(1, repository.AccessCalls);
        Assert.Equal("private, no-store", response.Headers.CacheControl.ToString());
        Assert.Equal("nosniff", response.Headers.XContentTypeOptions.ToString());

        var wrongScope = await Execute(context => StatutorySalesBookEndpoint.ReadAsync(
            annex, Request() with { FiscalIdentityId = Guid.NewGuid() }, new(repository), repository, context,
            new Host(Environments.Production), default));
        Assert.Equal(StatusCodes.Status404NotFound, wrongScope.StatusCode);
        Assert.Equal(1, repository.ReadCalls);
    }

    [Theory]
    [InlineData("xlsx", StatutorySalesBookContract.XlsxContentType, ".xlsx", "PK")]
    [InlineData("pdf", StatutorySalesBookContract.PdfContentType, ".pdf", "%PDF-")]
    public async Task ExportReturnsCanonicalArtifactAndRecordsOnlySafeScope(string format, string contentType, string extension, string signature)
    {
        var repository = new StubRepository(StatutorySalesBookType.AnnexE2);
        var artifacts = TestFiscalReportArtifacts.CaptureService(out var artifactRepository);
        var response = await Execute(context => StatutorySalesBookEndpoint.DownloadAsync(
            "e2", Request(), format, new(repository), new(), artifacts, repository, context, new Host(Environments.Production), default));

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(contentType, response.ContentType);
        Assert.Contains(extension, response.Headers.ContentDisposition.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(signature, System.Text.Encoding.ASCII.GetString(response.Body, 0, signature.Length), StringComparison.Ordinal);
        Assert.Equal(1, repository.ReadCalls);
        Assert.Equal(1, repository.ExportCalls);
        Assert.Equal("ANNEX_E2", repository.LastReportKind);
        Assert.Equal($"pos-server-v2/{Site:D}/{Identity:D}/PHP/2026-10", repository.LastSubjectReference);
        Assert.NotNull(artifactRepository.Command);
        Assert.Equal(StatutorySalesBookContract.ArtifactContractProfile, artifactRepository.Command.ArtifactVersion);
        Assert.Equal(repository.LastSubjectReference, artifactRepository.Command.ScopeReference);
        Assert.DoesNotContain("terminal", artifactRepository.Command.ScopeReference, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Synthetic Senior", repository.LastSubjectReference, StringComparison.Ordinal);
        Assert.Equal("private, no-store", response.Headers.CacheControl.ToString());
        Assert.Equal("nosniff", response.Headers.XContentTypeOptions.ToString());
    }

    [Theory]
    [InlineData("e2", StatutorySalesBookType.AnnexE2, "pdf", "92000000-0000-4000-8000-000000000101", "92000000-0000-4000-8000-000000000102")]
    [InlineData("e2", StatutorySalesBookType.AnnexE2, "xlsx", "92000000-0000-4000-8000-000000000103", "92000000-0000-4000-8000-000000000104")]
    [InlineData("e3", StatutorySalesBookType.AnnexE3, "pdf", "92000000-0000-4000-8000-000000000105", "92000000-0000-4000-8000-000000000106")]
    [InlineData("e3", StatutorySalesBookType.AnnexE3, "xlsx", "92000000-0000-4000-8000-000000000107", "92000000-0000-4000-8000-000000000108")]
    public async Task LayoutVersionCreatesImmutableSuccessorAndReusesCurrentArtifact(
        string annex,
        StatutorySalesBookType type,
        string format,
        string legacyArtifactId,
        string currentArtifactId)
    {
        var repository = new StubRepository(type, fullFeeExemption: true);
        var legacyBytes = Encoding.UTF8.GetBytes($"legacy-11-column-{annex}-{format}");
        var previousBytes = Encoding.UTF8.GetBytes($"previous-12-column-{annex}-{format}");
        var artifacts = new ImmutableRevisionRepository(
            Guid.Parse(legacyArtifactId),
            Guid.Parse(currentArtifactId),
            legacyBytes,
            previousBytes,
            type,
            format);
        var service = new FiscalReportArtifactService(artifacts);

        var created = await Execute(context => StatutorySalesBookEndpoint.DownloadAsync(
            annex, Request(), format, new(repository), new(), service, repository,
            context, new Host(Environments.Production), default));
        var replayed = await Execute(context => StatutorySalesBookEndpoint.DownloadAsync(
            annex, Request(), format, new(repository), new(), service, repository,
            context, new Host(Environments.Production), default));

        Assert.Equal(StatusCodes.Status200OK, created.StatusCode);
        Assert.Equal(created.Body, replayed.Body);
        Assert.Equal(1, artifacts.RenderCount);
        Assert.Equal(3, artifacts.Current.Revision);
        Assert.Equal(artifacts.Previous.ArtifactId, artifacts.Current.SupersedesArtifactId);
        Assert.Equal(StatutorySalesBookContract.ArtifactContractProfile, artifacts.Current.ArtifactVersion);
        Assert.NotEqual(artifacts.Previous.SourceFingerprint, artifacts.Current.SourceFingerprint);
        Assert.Equal(legacyBytes, artifacts.Read(artifacts.Legacy.ArtifactId));
        Assert.Equal(previousBytes, artifacts.Read(artifacts.Previous.ArtifactId));
        Assert.Equal(artifacts.Legacy.Sha256, Hash(legacyBytes));
        Assert.Equal(artifacts.Previous.Sha256, Hash(previousBytes));
        Assert.Equal(artifacts.Current.Sha256, Hash(created.Body));
        AssertCurrentSeventeenColumnOutput(created.Body, format);
    }

    [Fact]
    public async Task UnsupportedFormatAndWrongSiteFailBeforeReadOrAudit()
    {
        var repository = new StubRepository(StatutorySalesBookType.AnnexE2);
        var unsupported = await Execute(context => StatutorySalesBookEndpoint.DownloadAsync(
            "e2", Request(), "csv", new(repository), new(), TestFiscalReportArtifacts.Service(), repository, context, new Host(Environments.Production), default));
        var wrongSite = await Execute(context => StatutorySalesBookEndpoint.DownloadAsync(
            "e2", Request() with { SitePosServerId = Guid.NewGuid() }, "pdf", new(repository), new(), TestFiscalReportArtifacts.Service(), repository,
            context, new Host(Environments.Production), default));

        Assert.Equal(StatusCodes.Status400BadRequest, unsupported.StatusCode);
        Assert.Equal(StatusCodes.Status404NotFound, wrongSite.StatusCode);
        Assert.Equal(0, repository.ReadCalls);
        Assert.Equal(0, repository.ExportCalls);
    }

    [Fact]
    public async Task ExportFailsClosedWhenImmutableArtifactCannotBePersisted()
    {
        var repository = new StubRepository(StatutorySalesBookType.AnnexE2);
        var response = await Execute(context => StatutorySalesBookEndpoint.DownloadAsync(
            "e2", Request(), "xlsx", new(repository), new(), TestFiscalReportArtifacts.FailureService(), repository,
            context, new Host(Environments.Production), default));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.StatusCode);
        Assert.Equal(0, repository.ExportCalls);
    }

    private static StatutorySalesBookRequest Request() => new(Site, Identity, "PHP", 2026, 10);

    private static StatutorySalesBookReport Report(StatutorySalesBookType type, bool fullFeeExemption = false)
    {
        var generatedAt = DateTimeOffset.Parse("2026-10-17T08:30:00+08:00");
        var row = fullFeeExemption
            ? new StatutorySalesBookRow(Guid.Parse("91000000-0000-4000-8000-000000000010"), new DateOnly(2026, 10, 17), generatedAt,
                171, "Synthetic Beneficiary", type == StatutorySalesBookType.AnnexE2 ? "SENIOR_CITIZEN_ID" : "PWD_ID",
                "SYN-TEST-001", null, "SI-00000000000171", 5_000, 536, 0, 0, 0, 0, "City Ordinance No. 48", 0,
                "FREE_PARKING", type == StatutorySalesBookType.AnnexE2
                    ? "PH_PARANAQUE_SENIOR_FREE_PARKING"
                    : "PH_PARANAQUE_PWD_FREE_PARKING", 4_464, 4_464, 0, 0, 0,
                "FULL_FEE_EXEMPTION", null, 5_000, 5_000, 0,
                type == StatutorySalesBookType.AnnexE2
                    ? "PH_PARANAQUE_SENIOR_FREE_PARKING"
                    : "PH_PARANAQUE_PWD_FREE_PARKING")
            : new StatutorySalesBookRow(Guid.Parse("91000000-0000-4000-8000-000000000010"), new DateOnly(2026, 10, 17), generatedAt,
                117, "Synthetic Senior", "SENIOR_CITIZEN_ID", "SC-TEST-001", null, "SI-00000117",
                10000, 1071, 0, 0, 2000, 8000, "PARANAQUE-SYNTHETIC-20", 2000);
        return new(
            new("PITX TEST", "Paranaque City", "999-999-999-000", "ExitPass POS Server", "1.3", "v1.3", "2026-10-17",
                "SN-TEST", "MIN-TEST", "PITX-L3-POS-01", generatedAt, "operator"),
            new(type, Site, Identity, "PHP", 2026, 10, "PITX-L3-POS-01", 1,
                row.SalesInclusiveVatMinorUnits, row.VatAmountMinorUnits, row.VatExemptSalesMinorUnits,
                row.Discount5MinorUnits, row.Discount20MinorUnits, row.NetSalesMinorUnits, generatedAt, [],
                LguOriginalParkingFeeVatInclusiveMinorUnits: row.OriginalParkingFeeVatInclusiveMinorUnits,
                LguWaiverVatInclusiveMinorUnits: row.LguWaiverVatInclusiveMinorUnits,
                LguFinalPayableVatInclusiveMinorUnits: row.LguFinalPayableVatInclusiveMinorUnits),
            [row]);
    }

    private static void AssertCurrentSeventeenColumnOutput(byte[] bytes, string format)
    {
        if (format == "pdf")
        {
            var text = Encoding.Latin1.GetString(bytes);
            Assert.Contains("ADDITIONAL LGU PARKING-PRIVILEGE DETAIL", text);
            Assert.Contains("(SI-00000000000171) Tj", text);
            Assert.Contains("FULL_FEE_EXEMPTION", text);
            Assert.Contains("50.00", text);
            return;
        }

        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        using var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var worksheet = reader.ReadToEnd();
        Assert.Contains("ADDITIONAL LGU PARKING-PRIVILEGE DETAIL", worksheet);
        Assert.Contains("LGU Policy Reference", worksheet);
        Assert.Contains("SI-00000000000171", worksheet);
        Assert.Contains("<c r=\"O17\" s=\"4\"><v>50.00</v></c>", worksheet);
        Assert.Contains("ref=\"A1:Q25\"", worksheet);
    }

    private static string Hash(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static async Task<Capture> Execute(Func<DefaultHttpContext, Task<IResult>> action)
    {
        var context = new DefaultHttpContext();
        using var provider = new ServiceCollection().AddLogging().AddRouting().BuildServiceProvider();
        context.RequestServices = provider;
        context.Response.Body = new MemoryStream();
        context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName] = "statutory-sales-book-test";
        context.User = Principal(StatutorySalesBookAuthorization.ExportPermission);
        var result = await action(context);
        await result.ExecuteAsync(context);
        return new(context.Response.StatusCode, context.Response.ContentType, context.Response.Headers,
            ((MemoryStream)context.Response.Body).ToArray());
    }

    private static ClaimsPrincipal Principal(string permission) => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, "central-pms"),
        new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, permission),
        new Claim(BirSalesSummaryAuthorization.AuthorityClassClaimType, BirSalesSummaryAuthorization.ProductionAuthorityClass),
        new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, Site.ToString("D")),
        new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, Identity.ToString("D")),
        new Claim(FiscalZReadingAuthorization.CurrencyScopeClaimType, "PHP")
    ], SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));

    private static readonly Guid Site = Guid.Parse("91000000-0000-4000-8000-000000000001");
    private static readonly Guid Identity = Guid.Parse("91000000-0000-4000-8000-000000000002");
    private sealed record Capture(int StatusCode, string? ContentType, IHeaderDictionary Headers, byte[] Body);
    private sealed class Host(string name) : IHostEnvironment { public string EnvironmentName { get; set; } = name; public string ApplicationName { get; set; } = "tests"; public string ContentRootPath { get; set; } = "."; public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider(); }

    private sealed class StubRepository(
        StatutorySalesBookType type,
        bool fullFeeExemption = false) : IStatutorySalesBookRepository, IAuditTrailRepository
    {
        public int ReadCalls { get; private set; }
        public int AccessCalls { get; private set; }
        public int ExportCalls { get; private set; }
        public StatutorySalesBookQuery? Query { get; private set; }
        public string? LastReportKind { get; private set; }
        public string? LastSubjectReference { get; private set; }
        public Task<StatutorySalesBookResult> ReadAsync(StatutorySalesBookQuery query, CancellationToken cancellationToken = default) { ReadCalls++; Query = query; return Task.FromResult(new StatutorySalesBookResult(StatutorySalesBookOutcome.Succeeded, Report(type, fullFeeExemption))); }
        public Task<AuditTrailResult> ReadAsync(AuditTrailQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RecordAccessAsync(Guid sitePosServerId, string reportKind, string subjectReference, string actorReference, string correlationReference, int rowCount, CancellationToken cancellationToken = default) { AccessCalls++; LastReportKind = reportKind; LastSubjectReference = subjectReference; return Task.CompletedTask; }
        public Task RecordExportAsync(Guid sitePosServerId, string reportKind, string subjectReference, string format, string actorReference, string correlationReference, string contentSha256, int rowCount, CancellationToken cancellationToken = default) { ExportCalls++; LastReportKind = reportKind; LastSubjectReference = subjectReference; return Task.CompletedTask; }
    }

    private sealed class ImmutableRevisionRepository : IFiscalReportArtifactRepository
    {
        private readonly Dictionary<Guid, byte[]> bytesByArtifactId = [];
        private readonly Guid currentArtifactId;

        public ImmutableRevisionRepository(
            Guid legacyArtifactId,
            Guid currentArtifactId,
            byte[] legacyBytes,
            byte[] previousBytes,
            StatutorySalesBookType type,
            string format)
        {
            this.currentArtifactId = currentArtifactId;
            var legacyHash = Hash(legacyBytes);
            Legacy = Record(
                legacyArtifactId,
                type,
                format,
                "pos-server-statutory-sales-book-rmo24-2023:pos-server-scope-v2",
                FiscalReportArtifactContract.SourceFingerprint(new { LayoutVersion = "annex-e2-e3-11-column-v1", Scope = "2026-10" }),
                1,
                null,
                legacyHash,
                legacyBytes.LongLength);
            var previousHash = Hash(previousBytes);
            Previous = Record(
                Guid.Parse("93000000-0000-4000-8000-000000000012"),
                type,
                format,
                "pos-server-statutory-sales-book-rmo24-2023:pos-server-scope-v3:annex-e2-e3-12-column-v1",
                FiscalReportArtifactContract.SourceFingerprint(new { LayoutVersion = "annex-e2-e3-12-column-v1", Scope = "2026-10" }),
                2,
                Legacy.ArtifactId,
                previousHash,
                previousBytes.LongLength);
            Current = Previous;
            bytesByArtifactId.Add(Legacy.ArtifactId, legacyBytes.ToArray());
            bytesByArtifactId.Add(Previous.ArtifactId, previousBytes.ToArray());
        }

        public FiscalReportArtifactRecord Legacy { get; }
        public FiscalReportArtifactRecord Previous { get; }
        public FiscalReportArtifactRecord Current { get; private set; }
        public int RenderCount { get; private set; }

        public async Task<FiscalReportArtifactResult> GetOrCreateAsync(
            FiscalReportArtifactCommand command,
            Func<CancellationToken, Task<FiscalReportArtifactPayload>> render,
            CancellationToken cancellationToken = default)
        {
            if (string.Equals(Current.SourceFingerprint, command.SourceFingerprint, StringComparison.Ordinal))
                return new(FiscalReportArtifactOutcome.Reused, Current, Read(Current.ArtifactId));

            RenderCount++;
            var payload = await render(cancellationToken);
            var hash = Hash(payload.Bytes);
            Current = new(
                currentArtifactId, command.SitePosServerId, command.FiscalIdentityId, command.CurrencyCode,
                command.ReportType, command.ScopeReference, command.Format, command.ArtifactVersion,
                command.SourceFingerprint, command.FiscalReportingPeriodId, command.CalendarYear,
                command.CalendarMonth, command.BusinessDayDate, Previous.Revision + 1, Previous.ArtifactId,
                $"test/{hash}.{command.Format}", hash, payload.Bytes.LongLength, payload.ContentType,
                payload.FileName, DateTimeOffset.Parse("2026-11-03T10:00:00+08:00"));
            bytesByArtifactId.Add(Current.ArtifactId, payload.Bytes.ToArray());
            return new(FiscalReportArtifactOutcome.Created, Current, Read(Current.ArtifactId));
        }

        public byte[] Read(Guid artifactId) => bytesByArtifactId[artifactId].ToArray();

        private static FiscalReportArtifactRecord Record(
            Guid artifactId,
            StatutorySalesBookType type,
            string format,
            string version,
            string sourceFingerprint,
            int revision,
            Guid? supersedesArtifactId,
            string hash,
            long byteLength) => new(
                artifactId, Site, Identity, "PHP", type == StatutorySalesBookType.AnnexE2 ? "ANNEX_E2" : "ANNEX_E3",
                $"pos-server-v2/{Site:D}/{Identity:D}/PHP/2026-10",
                format, version, sourceFingerprint, null, 2026, 10, null, revision, supersedesArtifactId,
                $"test/{hash}.{format}", hash, byteLength,
                format == "pdf" ? StatutorySalesBookContract.PdfContentType : StatutorySalesBookContract.XlsxContentType,
                $"legacy-11-column.{format}", DateTimeOffset.Parse("2026-11-01T10:00:00+08:00"));
    }
}
