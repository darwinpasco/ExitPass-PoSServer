using System.Globalization;
using System.Text;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class AnnexE1AuthoritativeSourceDeriver
{
    public AnnexE1DerivedAccountingValues Derive(
        AnnexE1AutomaticSourceEvidence source,
        IReadOnlyDictionary<string, AnnexE1PeriodFactRecord> recordedFacts)
    {
        ValidateCompleteSource(source);

        var factIds = new List<Guid>(recordedFacts.Count);
        var evidenceHashes = new List<string>(AnnexE1FactTypes.Ordered.Count);
        var amounts = new Dictionary<string, long>(StringComparer.Ordinal);

        foreach (var type in AnnexE1FactTypes.Ordered)
        {
            if (recordedFacts.TryGetValue(type, out var fact))
            {
                ValidateRecordedFact(type, fact);
                amounts[type] = fact.AmountMinorUnits;
                factIds.Add(fact.FactId);
                evidenceHashes.Add(fact.SemanticHash);
                continue;
            }

            amounts[type] = DeriveZero(type, source);
            evidenceHashes.Add(DerivedEvidenceHash(type, source));
        }

        return new(
            amounts[AnnexE1FactTypes.ManualSiOrNetIncome],
            amounts[AnnexE1FactTypes.SalesOverrunOverflowNetIncome],
            amounts[AnnexE1FactTypes.NaacDiscount],
            amounts[AnnexE1FactTypes.SoloParentDiscount],
            amounts[AnnexE1FactTypes.OtherVatAdjustment],
            amounts[AnnexE1FactTypes.VatOnReturns],
            amounts[AnnexE1FactTypes.ResidualVatAdjustment],
            factIds,
            evidenceHashes);
    }

    private static void ValidateCompleteSource(AnnexE1AutomaticSourceEvidence source)
    {
        if (source.GoverningZReportId == Guid.Empty || source.BirSalesSummaryReportId == Guid.Empty ||
            source.BirSalesSummarySemanticHash.Length != 64)
            throw Missing("The committed Z-bound BIR Sales Summary identity is incomplete.");

        if (source.OtherStatutoryDiscount != 0)
            throw Missing("The committed source contains an other-statutory discount that is not authoritatively split into NAAC, Solo Parent, or another governed classification.");

        if (source.ReturnAmount != 0 || source.RefundAmount != 0)
            throw Missing("The committed source contains return or refund activity without a separately governed VAT-on-returns amount.");

        if (source.AdjustmentAmount != 0)
            throw Missing("The committed source contains adjustment activity without separately governed Annex E-1 VAT-adjustment classifications.");

        if (source.ServiceChargeAmount != 0)
            throw new AnnexE1SafeException(
                AnnexE1Outcome.UnsupportedClassification,
                "The committed source contains service-charge activity outside the bounded Annex E-1 profile.");

        if (source.ParkingVatExemptionAmount != 0)
            throw Missing("The committed source contains a VAT-exemption privilege amount on a statutory parking transaction. Parking discounts do not establish VAT exemption; the immutable fiscal record requires governed historical correction.");

        var classifiedVatExemption = checked(source.SeniorCitizenVatAdjustment + source.PwdVatAdjustment);
        if (source.VatExemptionAmount != classifiedVatExemption)
            throw Missing("The committed VAT-exemption total does not reconcile to the Senior Citizen and PWD VAT-adjustment breakdowns.");
    }

    private static long DeriveZero(string type, AnnexE1AutomaticSourceEvidence source)
    {
        if (type == AnnexE1FactTypes.SalesOverrunOverflowNetIncome)
        {
            var gtaMovement = checked(source.ResultingGta - source.PreviousGta);
            if (gtaMovement != source.NetSales)
                throw Missing("The accumulated grand-total movement does not prove zero sales overrun/overflow net income.");
        }

        return 0;
    }

    private static void ValidateRecordedFact(string type, AnnexE1PeriodFactRecord fact)
    {
        if (fact.SemanticHash.Length != 64)
            throw Missing($"The governed Annex E-1 fact '{type}' has invalid source integrity evidence.");

        if (fact.FactStatus == AnnexE1FactStatuses.AttestedZero)
        {
            if (fact.AmountMinorUnits != 0)
                throw Missing($"The governed Annex E-1 fact '{type}' has inconsistent zero evidence.");
            return;
        }

        if (fact.FactStatus == AnnexE1FactStatuses.Recorded &&
            type is AnnexE1FactTypes.ManualSiOrNetIncome or AnnexE1FactTypes.SalesOverrunOverflowNetIncome)
            return;

        throw new AnnexE1SafeException(
            AnnexE1Outcome.UnsupportedPrivilege,
            $"The governed Annex E-1 fact '{type}' uses an unsupported nonzero classification.");
    }

    private static string DerivedEvidenceHash(string type, AnnexE1AutomaticSourceEvidence source)
    {
        var canonical = string.Join('|',
            "annex-e1-authoritative-source:v1",
            type,
            source.GoverningZReportId.ToString("D"),
            source.BirSalesSummaryReportId.ToString("D"),
            source.BirSalesSummarySemanticHash,
            Number(source.PreviousGta),
            Number(source.ResultingGta),
            Number(source.GrossSales),
            Number(source.NetSales),
            Number(source.OtherStatutoryDiscount),
            Number(source.VatExemptionAmount),
            Number(source.SeniorCitizenVatAdjustment),
            Number(source.PwdVatAdjustment),
            Number(source.ParkingVatExemptionAmount),
            Number(source.ReturnAmount),
            Number(source.RefundAmount),
            Number(source.AdjustmentAmount),
            Number(source.ServiceChargeAmount));
        return AnnexE1SemanticHasher.Hash(Encoding.UTF8.GetBytes(canonical));
    }

    private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static AnnexE1SafeException Missing(string message) =>
        new(AnnexE1Outcome.MissingAuthoritativeSource, message);
}
