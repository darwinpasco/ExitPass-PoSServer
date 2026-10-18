using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using ExitPass.PosServer.Runtime.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.FiscalReports;

public sealed class AnnexE1RuntimeTests
{
    [Fact]
    public void ApprovedProfileIdentityAndExactCalculationsAreFrozen()
    {
        Assert.Equal("36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f",AnnexE1Contract.CalculationProfileSha256);
        var row=new AnnexE1CalculationEngine().Calculate(Input());var p=row.Positions.ToDictionary(x=>x.Position);
        Assert.Equal(15_000,p["D07"].MinorUnitsValue);Assert.Equal(2_000,p["D16"].MinorUnitsValue);
        Assert.Equal(4_000,p["D19"].MinorUnitsValue);Assert.Equal(500,p["D25"].MinorUnitsValue);
        Assert.Equal(2_000,p["D26"].MinorUnitsValue);Assert.Equal(9_000,p["D27"].MinorUnitsValue);
        Assert.Equal(9_700,p["D29"].MinorUnitsValue);Assert.Equal(32,row.Positions.Count);Assert.All(row.Reconciliations,r=>Assert.True(r.Passed));
    }

    [Fact]
    public void MissingFactsNonzeroPrivilegeExceptionalSourcesAndOverflowFailClosed()
    {
        var engine=new AnnexE1CalculationEngine();
        Assert.Equal(AnnexE1Outcome.MissingAccountingFact,Assert.Throws<AnnexE1SafeException>(()=>engine.Calculate(Input() with{FactIds=[],FactSemanticHashes=[]})).Outcome);
        Assert.Equal(AnnexE1Outcome.UnsupportedPrivilege,Assert.Throws<AnnexE1SafeException>(()=>engine.Calculate(Input() with{NaacDiscount=1})).Outcome);
        Assert.Equal(AnnexE1Outcome.UnsupportedClassification,Assert.Throws<AnnexE1SafeException>(()=>engine.Calculate(Input() with{RefundAmount=1})).Outcome);
        Assert.Equal(AnnexE1Outcome.ArithmeticOverflow,Assert.Throws<AnnexE1SafeException>(()=>engine.Calculate(Input() with{ActiveGross=long.MaxValue,VoidAmount=1})).Outcome);
    }

    [Fact]
    public void ApprovedNoActivityRequiresCompleteZeroPosture()
    {
        var zero=ZeroInput() with{PreviousGta=100,ResultingGta=100};
        var row=new AnnexE1CalculationEngine().Calculate(zero);Assert.Equal("NO_ACTIVITY",row.Positions.Single(p=>p.Position=="D32").TextValue);
        Assert.Equal(AnnexE1Outcome.ReconciliationFailure,Assert.Throws<AnnexE1SafeException>(()=>new AnnexE1CalculationEngine().Calculate(zero with{ResultingGta=101})).Outcome);
    }

    [Fact]
    public void XlsxIsByteIdenticalValidAndPreservesOfficialGeometryAndExpressions()
    {
        var renderer=new AnnexE1DeterministicXlsxRenderer();var header=Header();var rows=new[]{new AnnexE1CalculationEngine().Calculate(Input())};
        var first=renderer.Render(header,rows);var second=new AnnexE1DeterministicXlsxRenderer().Render(header,rows);
        Assert.Equal(first,second);Assert.Equal(SHA256.HashData(first),SHA256.HashData(second));
        using var archive=new ZipArchive(new MemoryStream(first),ZipArchiveMode.Read);Assert.Equal(10,archive.Entries.Count);
        Assert.All(archive.Entries,e=>Assert.Equal(new DateTime(2000,1,1),e.LastWriteTime.DateTime));
        foreach(var entry in archive.Entries.Where(e=>e.FullName.EndsWith(".xml",StringComparison.Ordinal)))using(var stream=entry.Open())XDocument.Load(stream);
        var sheet=Read(archive,"xl/worksheets/sheet1.xml");
        Assert.Contains("<t>BIR SALES SUMMARY REPORT</t>",sheet);
        Assert.DoesNotContain("BIR SALES SUMMARY  REPORT",sheet);
        Assert.Contains("A1:AF17",sheet);Assert.Contains("22 = 17+18+19+20+21",sheet);Assert.Contains("23 = 8-19",sheet);Assert.Contains("24 = 6-16-8",sheet);
        var workbook=Read(archive,"xl/workbook.xml");Assert.Contains("'E-1'!$A$1:$AF$17",workbook);
        Assert.Equal(1,XDocument.Parse(sheet).Descendants().Count(x=>x.Name.LocalName=="row"&&x.Attribute("r")?.Value=="17"));
        var zeroInput=ZeroInput();
        using var zeroArchive=new ZipArchive(new MemoryStream(renderer.Render(header,[new AnnexE1CalculationEngine().Calculate(zeroInput)])),ZipArchiveMode.Read);
        Assert.Contains("NO_ACTIVITY",Read(zeroArchive,"xl/worksheets/sheet1.xml"));
    }

    [Fact]
    public void PdfIsDeterministicAndPreservesCanonicalWorksheetGeometryAndValues()
    {
        var workbook=Workbook(10);var renderer=new AnnexE1PdfRenderer();
        var first=renderer.Render(workbook);var second=renderer.Render(workbook);
        Assert.Equal("application/pdf",first.ContentType);Assert.EndsWith(".pdf",first.FileName,StringComparison.Ordinal);
        Assert.Equal(first.Bytes,second.Bytes);Assert.Equal(first.Sha256,second.Sha256);Assert.StartsWith("%PDF-1.4",Encoding.Latin1.GetString(first.Bytes));
        var text=Encoding.Latin1.GetString(first.Bytes);
        Assert.Contains("/MediaBox [0 0 841.89 595.28]",text);Assert.Equal(1,Count(text,"/Type /Page "));
        Assert.Contains("BIR SALES SUMMARY REPORT",text);Assert.Contains("ANNEX E-1",text);
        Assert.DoesNotContain("ANNEX E-1 PDF PRESENTATION",text);Assert.DoesNotContain("TECHNICAL UAT",text);
        foreach(var heading in new[]{"Date","Beginning","Ending","SI/OR","Grand","Accum.","Gross","Deductions","Adjustment","VAT","Payable","Net","Sales","Overrun","/Overflow","Total","Income","Reset","Counter","Z","-Counter","Remarks"})
            Assert.Contains(heading,text);
        Assert.Contains("2026-08",text);Assert.Contains("-01",text);Assert.Contains("SI-1",text);Assert.Contains("150.00",text);
        Assert.Equal(10,workbook.Rows.Count);Assert.All(workbook.Rows,row=>Assert.Equal(32,row.Positions.Count));
    }

    [Fact]
    public async Task ServiceSeparatesRecordedFactsFromKnownZeroAndCorrectionLineage()
    {
        var repository=new Stub();var service=new AnnexE1Service(repository);
        var zero=Fact() with{FactStatus=AnnexE1FactStatuses.AttestedZero,AmountMinorUnits=1};Assert.Equal(AnnexE1Outcome.InvalidRequest,(await service.RecordFactAsync(zero)).Outcome);
        Assert.Equal(AnnexE1Outcome.InvalidRequest,(await service.RecordFactAsync(Fact() with{SourceDocumentCount=0})).Outcome);
        Assert.Equal(AnnexE1Outcome.UnsupportedPrivilege,(await service.RecordFactAsync(Fact() with{FactType=AnnexE1FactTypes.NaacDiscount})).Outcome);
        var correction=await service.GenerateAsync(Generation() with{SupersedesWorkbookId=Guid.NewGuid()});Assert.Equal(AnnexE1Outcome.InvalidRequest,correction.Outcome);
        Assert.Equal(AnnexE1Outcome.Created,(await service.RecordFactAsync(Fact())).Outcome);
    }

    [Fact]
    public async Task CurrentWorkbookReadValidatesExactGovernedScope()
    {
        var repository=new Stub();var service=new AnnexE1Service(repository);
        var query=new AnnexE1CurrentWorkbookQuery(Guid.NewGuid(),Guid.NewGuid(),"PHP",2026,10,AnnexE1Contract.Profile);
        Assert.Equal(AnnexE1Outcome.Replayed,(await service.GetCurrentAsync(query)).Outcome);
        Assert.Equal(query,repository.CurrentQuery);
        Assert.Equal(AnnexE1Outcome.InvalidRequest,(await service.GetCurrentAsync(query with{CalendarMonth=13})).Outcome);
        Assert.Equal(AnnexE1Outcome.InvalidRequest,(await service.GetCurrentAsync(query with{Profile=""})).Outcome);
    }

    [Fact]
    public void SemanticHashIsStableAcrossOperationActorCorrelationAndGenerationTime()
    {
        var source=Input();var command=Generation();var header=Header();var first=AnnexE1SemanticHasher.ComputeWorkbook(command,[source],header);
        var second=AnnexE1SemanticHasher.ComputeWorkbook(command with{OperationKey="retry",RequestedByRef="other",CorrelationId="other"},[source],header with{GeneratedAt=header.GeneratedAt.AddDays(1),GeneratedByRef="other"});
        Assert.Equal(first,second);Assert.Matches("^[0-9a-f]{64}$",first);
    }

    [Fact]
    public void AuthoritativeCommittedSourcesDeriveAllSevenZerosWithoutAttestations()
    {
        var values=new AnnexE1AuthoritativeSourceDeriver().Derive(AutomaticSource(),new Dictionary<string,AnnexE1PeriodFactRecord>());

        Assert.Equal(0,values.ManualNetIncome);Assert.Equal(0,values.OverflowNetIncome);
        Assert.Equal(0,values.NaacDiscount);Assert.Equal(0,values.SoloParentDiscount);
        Assert.Equal(0,values.OtherVatAdjustment);Assert.Equal(0,values.VatOnReturns);
        Assert.Equal(0,values.ResidualVatAdjustment);Assert.Empty(values.FactIds);
        Assert.Equal(7,values.EvidenceSemanticHashes.Count);Assert.All(values.EvidenceSemanticHashes,hash=>Assert.Matches("^[0-9a-f]{64}$",hash));
    }

    [Fact]
    public void RecordedManualAndOverflowFactsRemainAuthoritativeWhileZeroAttestationsAreOptional()
    {
        var manual=PeriodFact(AnnexE1FactTypes.ManualSiOrNetIncome,250);
        var overflow=PeriodFact(AnnexE1FactTypes.SalesOverrunOverflowNetIncome,75);
        var values=new AnnexE1AuthoritativeSourceDeriver().Derive(AutomaticSource(),new Dictionary<string,AnnexE1PeriodFactRecord>
        {
            [manual.FactType]=manual,[overflow.FactType]=overflow
        });

        Assert.Equal(250,values.ManualNetIncome);Assert.Equal(75,values.OverflowNetIncome);
        Assert.Equal([manual.FactId,overflow.FactId],values.FactIds);
    }

    [Theory]
    [InlineData("other_statutory")]
    [InlineData("return")]
    [InlineData("adjustment")]
    [InlineData("vat_breakdown")]
    [InlineData("gta")]
    public void IncompleteOrInconsistentAutomaticSourcesFailDeterministically(string defect)
    {
        var source=defect switch
        {
            "other_statutory"=>AutomaticSource() with{OtherStatutoryDiscount=1},
            "return"=>AutomaticSource() with{ReturnAmount=1},
            "adjustment"=>AutomaticSource() with{AdjustmentAmount=1},
            "vat_breakdown"=>AutomaticSource() with{VatExemptionAmount=1},
            _=>AutomaticSource() with{ResultingGta=10_999}
        };

        Assert.Equal(AnnexE1Outcome.MissingAuthoritativeSource,
            Assert.Throws<AnnexE1SafeException>(()=>new AnnexE1AuthoritativeSourceDeriver().Derive(source,new Dictionary<string,AnnexE1PeriodFactRecord>())).Outcome);
    }

    [Fact]
    public void RevisionPlannerReplaysUnchangedContentAndOnlyAutoSupersedesNewlyClosedPeriods()
    {
        var firstRow=new AnnexE1CalculationEngine().Calculate(Input());
        var current=Workbook() with{SemanticHash=new string('c',64),Rows=[firstRow]};
        Assert.Equal(AnnexE1RevisionDecision.Replay,AnnexE1RevisionPlanner.Decide(current,new string('c',64),[firstRow]));
        Assert.Equal(AnnexE1RevisionDecision.Replay,AnnexE1RevisionPlanner.Decide(current,new string('e',64),[firstRow]));

        var nextRow=new AnnexE1CalculationEngine().Calculate(Input() with
        {
            FiscalReportingPeriodId=Guid.NewGuid(),PeriodSequence=2,GoverningZReportId=Guid.NewGuid(),
            BirSalesSummaryReportId=Guid.NewGuid(),BusinessDayDate=new(2026,8,2)
        });
        Assert.Equal(AnnexE1RevisionDecision.AutomaticSuccessor,AnnexE1RevisionPlanner.Decide(current,new string('e',64),[firstRow,nextRow]));
        var changedRow=new AnnexE1CalculationEngine().Calculate(Input() with{ManualNetIncome=1});
        Assert.Equal(AnnexE1RevisionDecision.ExplicitCorrectionRequired,AnnexE1RevisionPlanner.Decide(current,new string('e',64),[changedRow]));
    }

    private static AnnexE1RowInputs Input()=>new(
        FiscalReportingPeriodId:Guid.NewGuid(),PeriodSequence:1,GoverningZReportId:Guid.NewGuid(),GoverningZReference:"Z-1",
        BirSalesSummaryReportId:Guid.NewGuid(),BirSalesSummarySemanticHash:new string('a',64),BusinessDayDate:new(2026,8,1),
        BeginningFiscalNumber:"SI-1",EndingFiscalNumber:"SI-1",PreviousGta:1_000,ResultingGta:11_000,ManualNetIncome:500,
        ActiveGross:14_000,ReturnAmount:0,VoidAmount:1_000,VatableSales:10_000,VatAmount:2_000,VatExemptSales:0,
        ZeroRatedSales:0,SeniorCitizenDiscount:1_000,PwdDiscount:0,NaacDiscount:0,SoloParentDiscount:0,
        OtherStatutoryDiscount:2_000,CouponDiscount:0,PromotionalDiscount:0,SeniorCitizenVatAdjustment:300,
        PwdVatAdjustment:200,OtherVatAdjustment:0,VatOnReturns:0,ResidualVatAdjustment:0,OverflowNetIncome:200,
        ResetCounter:0,ZCounter:1,TransactionCount:1,RefundAmount:0,AdjustmentAmount:0,ServiceChargeAmount:0,
        FiscalRangeCount:1,FiscalGapCount:0,FactIds:Enumerable.Range(0,7).Select(_=>Guid.NewGuid()).ToArray(),
        FactSemanticHashes:Enumerable.Range(0,7).Select(_=>new string('b',64)).ToArray());
    private static AnnexE1Header Header()=>new("Synthetic Taxpayer","Synthetic Address","000-000-000","ExitPass POS Server","1.3","Z-012B","2026-08-10","SERIAL","MIN","SITE-01",DateTimeOffset.Parse("2026-08-10T00:00:00Z"),"actor");
    private static AnnexE1WorkbookRecord Workbook(int rowCount=1)=>new(
        Guid.Parse("613af721-3b65-4bc4-9ce3-d2a032c26ab3"),"AE1-202608-TEST-R002","operation",AnnexE1Contract.Profile,
        AnnexE1Contract.CalculationProfile,AnnexE1Contract.RendererVersion,Guid.NewGuid(),Guid.NewGuid(),"PHP",2026,8,2,
        new string('c',64),new string('d',64),1234,AnnexE1Contract.MimeType,"annex-e1-test-r002.xlsx",
        Header() with{PosSerialNumber="SN-TEST",MachineIdentificationNumber="MIN-TEST"},
        Enumerable.Range(0,rowCount).Select(index=>new AnnexE1CalculationEngine().Calculate(Input() with{PeriodSequence=index+1})).ToArray(),
        Guid.NewGuid(),null,"presentation_correction","approval",DateTimeOffset.Parse("2026-09-01T00:00:00Z"),
        DateTimeOffset.Parse("2026-09-01T00:00:00Z"),"correlation");
    private static AnnexE1RowInputs ZeroInput()=>Input() with{TransactionCount=0,ActiveGross=0,ReturnAmount=0,VoidAmount=0,VatableSales=0,VatAmount=0,VatExemptSales=0,ZeroRatedSales=0,SeniorCitizenDiscount=0,PwdDiscount=0,NaacDiscount=0,SoloParentDiscount=0,OtherStatutoryDiscount=0,CouponDiscount=0,PromotionalDiscount=0,SeniorCitizenVatAdjustment=0,PwdVatAdjustment=0,OtherVatAdjustment=0,VatOnReturns=0,ResidualVatAdjustment=0,ManualNetIncome=0,OverflowNetIncome=0,PreviousGta=0,ResultingGta=0,FiscalRangeCount=0,FiscalGapCount=0,BeginningFiscalNumber=null,EndingFiscalNumber=null};
    private static AnnexE1PeriodFactCommand Fact()=>new("fact",Guid.NewGuid(),Guid.NewGuid(),"PHP",Guid.NewGuid(),AnnexE1FactTypes.ManualSiOrNetIncome,AnnexE1FactStatuses.Recorded,100,1,"MANUAL-1","MANUAL-1",null,"approval",null,null,"actor","service","correlation");
    private static AnnexE1AutomaticSourceEvidence AutomaticSource()=>new(Guid.NewGuid(),Guid.NewGuid(),new string('a',64),1_000,11_000,12_000,10_000,0,500,300,200,0,0,0,0);
    private static AnnexE1PeriodFactRecord PeriodFact(string type,long amount)=>new(Guid.NewGuid(),"fact",Guid.NewGuid(),Guid.NewGuid(),"PHP",Guid.NewGuid(),new(2026,8,1),type,AnnexE1FactStatuses.Recorded,amount,1,"SRC-1","SRC-1",type==AnnexE1FactTypes.SalesOverrunOverflowNetIncome?"EVENT-1":null,"approval",new string('b',64),null,null,DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,"actor","service","correlation");
    private static AnnexE1GenerationCommand Generation()=>new("generate",Guid.NewGuid(),Guid.NewGuid(),"PHP",2026,8,AnnexE1Contract.Profile,null,null,null,"actor","service","correlation");
    private static string Read(ZipArchive archive,string name){using var reader=new StreamReader(archive.GetEntry(name)!.Open(),Encoding.UTF8);return reader.ReadToEnd();}
    private static int Count(string value,string token){var count=0;var offset=0;while((offset=value.IndexOf(token,offset,StringComparison.Ordinal))>=0){count++;offset+=token.Length;}return count;}
    private sealed class Stub:IAnnexE1Repository{public AnnexE1CurrentWorkbookQuery? CurrentQuery;public Task<AnnexE1FactResult> RecordFactAsync(AnnexE1PeriodFactCommand c,CancellationToken t=default)=>Task.FromResult(new AnnexE1FactResult(AnnexE1Outcome.Created,new(Guid.NewGuid(),c.OperationKey,c.SitePosServerId,c.FiscalIdentityId,c.CurrencyCode,c.FiscalReportingPeriodId,new(2026,8,1),c.FactType,c.FactStatus,c.AmountMinorUnits,c.SourceDocumentCount,c.FirstSourceReference,c.LastSourceReference,c.SourceEventReference,c.ApprovalReference,new string('a',64),c.SupersedesFactId,c.CorrectionReason,DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,c.RequestedByRef,c.ServiceIdentityRef,c.CorrelationId)));public Task<AnnexE1WorkbookResult> GenerateAsync(AnnexE1GenerationCommand c,CancellationToken t=default)=>Task.FromResult(new AnnexE1WorkbookResult(AnnexE1Outcome.Created));public Task<AnnexE1WorkbookResult> GetCurrentAsync(AnnexE1CurrentWorkbookQuery q,CancellationToken t=default){CurrentQuery=q;return Task.FromResult(new AnnexE1WorkbookResult(AnnexE1Outcome.Replayed));}public Task<AnnexE1WorkbookResult> GetAsync(Guid i,CancellationToken t=default)=>throw new NotImplementedException();public Task<AnnexE1ArtifactResult> DownloadAsync(Guid i,CancellationToken t=default)=>throw new NotImplementedException();}
}
