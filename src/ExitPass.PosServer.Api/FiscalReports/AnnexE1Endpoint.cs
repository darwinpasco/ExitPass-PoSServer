using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed record AnnexE1FactRequest(string? OperationKey,Guid? SitePosServerId,Guid? FiscalIdentityId,string? CurrencyCode,Guid? FiscalReportingPeriodId,string? FactType,string? FactStatus,long? AmountMinorUnits,long? SourceDocumentCount,string? FirstSourceReference,string? LastSourceReference,string? SourceEventReference,string? ApprovalReference,Guid? SupersedesFactId,string? CorrectionReason);
public sealed record AnnexE1GenerationRequest(string? OperationKey,Guid? SitePosServerId,Guid? FiscalIdentityId,string? CurrencyCode,int? CalendarYear,int? CalendarMonth,string? Profile,Guid? SupersedesWorkbookId,string? CorrectionReason,string? CorrectionApprovalReference);
public sealed record AnnexE1ApiResponse(bool Succeeded,string Code,string CorrelationId,int HttpStatusCode,AnnexE1PeriodFactRecord? Fact=null,AnnexE1WorkbookRecord? Workbook=null,string? Message=null);
public sealed class AnnexE1Audit;

public static class AnnexE1Endpoint
{
    public static async Task<AnnexE1ApiResponse> RecordFactAsync(AnnexE1FactRequest request,AnnexE1Service service,HttpContext context,IHostEnvironment environment,bool zeroAttestation,CancellationToken ct)
    {
        var correlation=Correlation(context);
        if(!AnnexE1Authorization.IsHostingAuthorityAllowed(context.User,environment)){Audit(context,"fact","authority_denied",correlation);return Error("annex_e1_authority_denied",403,correlation);}
        if(request.SitePosServerId is null||request.FiscalIdentityId is null||request.FiscalReportingPeriodId is null||request.CurrencyCode is null||!AnnexE1Authorization.IsInScope(context.User,request.SitePosServerId.Value,request.FiscalIdentityId.Value,request.CurrencyCode)){Audit(context,"fact","scope_denied",correlation);return Error("annex_e1_scope_unavailable",404,correlation);}
        if(zeroAttestation!=string.Equals(request.FactStatus,AnnexE1FactStatuses.AttestedZero,StringComparison.Ordinal)){Audit(context,"fact","invalid_status",correlation);return Error("annex_e1_fact_status_invalid",400,correlation);}
        var result=await service.RecordFactAsync(new(request.OperationKey??"",request.SitePosServerId.Value,request.FiscalIdentityId.Value,request.CurrencyCode,request.FiscalReportingPeriodId.Value,request.FactType??"",request.FactStatus??"",request.AmountMinorUnits??-1,request.SourceDocumentCount??-1,request.FirstSourceReference,request.LastSourceReference,request.SourceEventReference,request.ApprovalReference??"",request.SupersedesFactId,request.CorrectionReason,Actor(context.User),Service(context.User),correlation),ct).ConfigureAwait(false);
        Audit(context,"fact",result.Outcome.ToString(),correlation);
        return Map(result,correlation);
    }

    public static async Task<AnnexE1ApiResponse> GenerateAsync(AnnexE1GenerationRequest request,AnnexE1Service service,HttpContext context,IHostEnvironment environment,CancellationToken ct)
    {
        var correlation=Correlation(context);
        if(!AnnexE1Authorization.IsHostingAuthorityAllowed(context.User,environment)){Audit(context,"generate","authority_denied",correlation);return Error("annex_e1_authority_denied",403,correlation);}
        if(request.SitePosServerId is null||request.FiscalIdentityId is null||request.CurrencyCode is null||!AnnexE1Authorization.IsInScope(context.User,request.SitePosServerId.Value,request.FiscalIdentityId.Value,request.CurrencyCode)){Audit(context,"generate","scope_denied",correlation);return Error("annex_e1_scope_unavailable",404,correlation);}
        if(request.SupersedesWorkbookId is not null&&!AnnexE1Authorization.HasPermission(context.User,AnnexE1Authorization.CorrectPermission)){Audit(context,"generate","correction_denied",correlation);return Error("annex_e1_correction_denied",403,correlation);}
        var result=await service.GenerateAsync(new(request.OperationKey??"",request.SitePosServerId.Value,request.FiscalIdentityId.Value,request.CurrencyCode,request.CalendarYear??0,request.CalendarMonth??0,request.Profile??"",request.SupersedesWorkbookId,request.CorrectionReason,request.CorrectionApprovalReference,Actor(context.User),Service(context.User),correlation),ct).ConfigureAwait(false);
        Audit(context,"generate",result.Outcome.ToString(),correlation);
        return Map(result,correlation);
    }

    public static async Task<AnnexE1ApiResponse> GetAsync(Guid id,AnnexE1Service service,HttpContext context,IHostEnvironment environment,CancellationToken ct)
    {
        var correlation=Correlation(context);if(!AnnexE1Authorization.IsHostingAuthorityAllowed(context.User,environment)){Audit(context,"read","authority_denied",correlation);return Error("annex_e1_authority_denied",403,correlation);}var result=await service.GetAsync(id,ct).ConfigureAwait(false);if(result.Workbook is not null&&!AnnexE1Authorization.IsInScope(context.User,result.Workbook.SitePosServerId,result.Workbook.FiscalIdentityId,result.Workbook.CurrencyCode)){Audit(context,"read","scope_denied",correlation);return Error("annex_e1_not_found",404,correlation);}Audit(context,"read",result.Outcome.ToString(),correlation);return Map(result,correlation);
    }

    public static async Task<IResult> DownloadAsync(Guid id,AnnexE1Service service,HttpContext context,IHostEnvironment environment,CancellationToken ct)
    {
        var correlation=Correlation(context);if(!AnnexE1Authorization.IsHostingAuthorityAllowed(context.User,environment)){Audit(context,"download","authority_denied",correlation);return Results.NotFound();}var result=await service.DownloadAsync(id,ct).ConfigureAwait(false);if(result.Workbook is null||result.Bytes is null||!AnnexE1Authorization.IsInScope(context.User,result.Workbook.SitePosServerId,result.Workbook.FiscalIdentityId,result.Workbook.CurrencyCode)){Audit(context,"download","not_found_or_denied",correlation);return Results.NotFound();}Audit(context,"download","replayed",correlation);context.Response.Headers.CacheControl="private, no-store";context.Response.Headers.ETag=$"\"sha256-{result.Workbook.ArtifactSha256}\"";context.Response.Headers.XContentTypeOptions="nosniff";return Results.File(result.Bytes,result.Workbook.MimeType,result.Workbook.FileName,enableRangeProcessing:false);
    }

    private static AnnexE1ApiResponse Map(AnnexE1FactResult r,string c)=>r.Outcome switch{AnnexE1Outcome.Created=>new(true,"annex_e1_fact_recorded",c,201,r.Fact),AnnexE1Outcome.Replayed=>new(true,"annex_e1_fact_replayed",c,200,r.Fact),AnnexE1Outcome.Conflict=>Error("annex_e1_semantic_conflict",409,c,r.SafeMessage),AnnexE1Outcome.ScopeMismatch or AnnexE1Outcome.NotFound=>Error("annex_e1_scope_unavailable",404,c),AnnexE1Outcome.UnsupportedPrivilege=>Error("annex_e1_privilege_unsupported",422,c,r.SafeMessage),AnnexE1Outcome.InvalidRequest=>Error("annex_e1_invalid_request",400,c,r.SafeMessage),_=>Error("annex_e1_unavailable",503,c)};
    private static AnnexE1ApiResponse Map(AnnexE1WorkbookResult r,string c)=>r.Outcome switch{AnnexE1Outcome.Created=>new(true,"annex_e1_generated",c,201,Workbook:r.Workbook),AnnexE1Outcome.Replayed=>new(true,"annex_e1_replayed",c,200,Workbook:r.Workbook),AnnexE1Outcome.Conflict=>Error("annex_e1_semantic_conflict",409,c,r.SafeMessage),AnnexE1Outcome.PeriodNotClosed=>Error("annex_e1_period_not_closed",409,c,r.SafeMessage),AnnexE1Outcome.GoverningZNotCommitted=>Error("annex_e1_z_not_committed",409,c,r.SafeMessage),AnnexE1Outcome.MissingAccountingFact or AnnexE1Outcome.MissingAuthoritativeSource=>Error("annex_e1_source_missing",422,c,r.SafeMessage),AnnexE1Outcome.UnsupportedPrivilege=>Error("annex_e1_privilege_unsupported",422,c,r.SafeMessage),AnnexE1Outcome.UnsupportedClassification or AnnexE1Outcome.ReconciliationFailure or AnnexE1Outcome.ArithmeticOverflow=>Error("annex_e1_generation_rejected",422,c,r.SafeMessage),AnnexE1Outcome.NotFound or AnnexE1Outcome.ScopeMismatch=>Error("annex_e1_not_found",404,c),AnnexE1Outcome.InvalidRequest=>Error("annex_e1_invalid_request",400,c,r.SafeMessage),_=>Error("annex_e1_unavailable",503,c,r.SafeMessage)};
    private static AnnexE1ApiResponse Error(string code,int status,string correlation,string? message=null)=>new(false,code,correlation,status,Message:message);
    private static string Correlation(HttpContext c)=>c.TraceIdentifier.Length<=200?c.TraceIdentifier:Guid.NewGuid().ToString("N");
    private static string Actor(ClaimsPrincipal p)=>p.FindFirstValue(ClaimTypes.NameIdentifier)??"authenticated-operator";
    private static string Service(ClaimsPrincipal p)=>p.FindFirstValue("client_id")??"pos-server-api";
    private static void Audit(HttpContext context,string action,string result,string correlation)
    {
        context.RequestServices?.GetService<ILogger<AnnexE1Audit>>()?.LogInformation("Annex E-1 {Action} {Result}; correlation {Correlation}",action,result,correlation);
    }
}
