namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class AnnexE1CalculationEngine
{
    public AnnexE1Row Calculate(AnnexE1RowInputs input)
    {
        try
        {
            ValidateSource(input);
            var d07 = checked(input.ActiveGross + input.ReturnAmount + input.VoidAmount);
            var d16 = checked(input.OtherStatutoryDiscount + input.CouponDiscount + input.PromotionalDiscount);
            var d19 = checked(input.SeniorCitizenDiscount + input.PwdDiscount + input.NaacDiscount +
                              input.SoloParentDiscount + d16 + input.ReturnAmount + input.VoidAmount);
            var d25 = checked(input.SeniorCitizenVatAdjustment + input.PwdVatAdjustment + input.OtherVatAdjustment +
                              input.VatOnReturns + input.ResidualVatAdjustment);
            var d26 = checked(input.VatAmount - input.OtherVatAdjustment);
            var d27 = checked(d07 - d19 - input.VatAmount);
            var d29 = checked(d27 + input.ManualNetIncome + input.OverflowNetIncome);
            if (d26 < 0 || d27 < 0 || d29 < 0)
                throw new AnnexE1SafeException(AnnexE1Outcome.ReconciliationFailure, "Annex E-1 calculated values do not reconcile to nonnegative governed amounts.");

            var noActivity = input.TransactionCount == 0;
            if (noActivity)
            {
                var amountTotal = checked(input.ActiveGross + input.ReturnAmount + input.VoidAmount + input.VatableSales +
                    input.VatAmount + input.VatExemptSales + input.ZeroRatedSales + input.SeniorCitizenDiscount +
                    input.PwdDiscount + input.NaacDiscount + input.SoloParentDiscount + d16 + d25 +
                    input.ManualNetIncome + input.OverflowNetIncome);
                if (amountTotal != 0 || input.PreviousGta != input.ResultingGta || input.FiscalRangeCount != 0 ||
                    input.BeginningFiscalNumber is not null || input.EndingFiscalNumber is not null)
                    throw new AnnexE1SafeException(AnnexE1Outcome.ReconciliationFailure, "The no-activity Annex E-1 source does not satisfy the approved zero posture.");
            }
            else if (input.FiscalRangeCount != 1 || string.IsNullOrWhiteSpace(input.BeginningFiscalNumber) || string.IsNullOrWhiteSpace(input.EndingFiscalNumber))
                throw new AnnexE1SafeException(AnnexE1Outcome.UnsupportedClassification, "The Annex E-1 fiscal-number range cannot be represented without loss.");

            var positions = new List<AnnexE1PositionValue>(32)
            {
                Text("D01", "business_date", input.BusinessDayDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)),
                Text("D02", "beginning_fiscal_number", input.BeginningFiscalNumber),
                Text("D03", "ending_fiscal_number", input.EndingFiscalNumber),
                Money("D04", "gta_ending", input.ResultingGta), Money("D05", "gta_beginning", input.PreviousGta),
                Money("D06", "manual_net_income", input.ManualNetIncome), Money("D07", "annex_gross_sales", d07),
                Money("D08", "vatable_sales", input.VatableSales), Money("D09", "recorded_vat_amount", input.VatAmount),
                Money("D10", "vat_exempt_sales", input.VatExemptSales), Money("D11", "zero_rated_sales", input.ZeroRatedSales),
                Money("D12", "sc_discount", input.SeniorCitizenDiscount), Money("D13", "pwd_discount", input.PwdDiscount),
                Money("D14", "naac_discount", input.NaacDiscount), Money("D15", "solo_parent_discount", input.SoloParentDiscount),
                Money("D16", "other_discount", d16), Money("D17", "return_deduction", input.ReturnAmount),
                Money("D18", "void_deduction", input.VoidAmount), Money("D19", "total_deductions", d19),
                Money("D20", "sc_vat_adjustment", input.SeniorCitizenVatAdjustment), Money("D21", "pwd_vat_adjustment", input.PwdVatAdjustment),
                Money("D22", "other_vat_adjustment", input.OtherVatAdjustment), Money("D23", "vat_on_returns", input.VatOnReturns),
                Money("D24", "vat_adjustment_other", input.ResidualVatAdjustment), Money("D25", "total_vat_adjustment", d25),
                Money("D26", "annex_vat_payable", d26), Money("D27", "annex_net_sales_ex_vat", d27),
                Money("D28", "overflow_net_income", input.OverflowNetIncome), Money("D29", "total_income", d29),
                Integer("D30", "reset_counter", input.ResetCounter), Integer("D31", "z_counter", input.ZCounter),
                Text("D32", "remarks", noActivity ? "NO_ACTIVITY" : "NONE")
            };

            var reconciliations = new[]
            {
                Rule("R01_D07", checked(d07 - input.ReturnAmount - input.VoidAmount - input.ActiveGross)),
                Rule("R02_D16", checked(d16 - input.OtherStatutoryDiscount - input.CouponDiscount - input.PromotionalDiscount)),
                Rule("R03_D19", checked(d19 - input.SeniorCitizenDiscount - input.PwdDiscount - input.NaacDiscount - input.SoloParentDiscount - d16 - input.ReturnAmount - input.VoidAmount)),
                Rule("R04_D25", checked(d25 - input.SeniorCitizenVatAdjustment - input.PwdVatAdjustment - input.OtherVatAdjustment - input.VatOnReturns - input.ResidualVatAdjustment)),
                Rule("R05_D26", checked(d26 + input.OtherVatAdjustment - input.VatAmount)),
                Rule("R06_D27", checked(d27 + d19 + input.VatAmount - d07)),
                Rule("R07_D29", checked(d29 - input.ManualNetIncome - input.OverflowNetIncome - d27))
            };
            if (reconciliations.Any(rule => !rule.Passed))
                throw new AnnexE1SafeException(AnnexE1Outcome.ReconciliationFailure, "Annex E-1 reconciliation failed with nonzero minor-unit variance.");
            return new(input, positions, reconciliations);
        }
        catch (OverflowException)
        {
            throw new AnnexE1SafeException(AnnexE1Outcome.ArithmeticOverflow, "Annex E-1 checked minor-unit arithmetic exceeded supported bounds.");
        }
    }

    private static void ValidateSource(AnnexE1RowInputs input)
    {
        foreach (var amount in new[] { input.PreviousGta, input.ResultingGta, input.ManualNetIncome, input.ActiveGross,
            input.ReturnAmount, input.VoidAmount, input.VatableSales, input.VatAmount, input.VatExemptSales,
            input.ZeroRatedSales, input.SeniorCitizenDiscount, input.PwdDiscount, input.NaacDiscount,
            input.SoloParentDiscount, input.OtherStatutoryDiscount, input.CouponDiscount, input.PromotionalDiscount,
            input.SeniorCitizenVatAdjustment, input.PwdVatAdjustment, input.OtherVatAdjustment, input.VatOnReturns,
            input.ResidualVatAdjustment, input.OverflowNetIncome, input.ResetCounter, input.ZCounter,
            input.TransactionCount, input.RefundAmount, input.AdjustmentAmount, input.ServiceChargeAmount,
            input.FiscalRangeCount, input.FiscalGapCount })
            if (amount < 0) throw new AnnexE1SafeException(AnnexE1Outcome.UnsupportedClassification, "Annex E-1 does not accept negative or unresolved governed source values.");

        if (input.RefundAmount != 0 || input.AdjustmentAmount != 0 || input.ServiceChargeAmount != 0 || input.ReturnAmount != 0)
            throw new AnnexE1SafeException(AnnexE1Outcome.UnsupportedClassification, "The Annex E-1 bounded profile does not support this exceptional source classification.");
        if (input.NaacDiscount != 0 || input.SoloParentDiscount != 0 || input.OtherVatAdjustment != 0 ||
            input.VatOnReturns != 0 || input.ResidualVatAdjustment != 0)
            throw new AnnexE1SafeException(AnnexE1Outcome.UnsupportedPrivilege, "A nonzero unresolved Annex E-1 privilege or VAT-adjustment source is not authorized.");
        if (input.FiscalGapCount != 0)
            throw new AnnexE1SafeException(AnnexE1Outcome.UnsupportedClassification, "Annex E-1 generation requires a gap-free representable fiscal range.");
        if (input.FactIds.Count != 7 || input.FactSemanticHashes.Count != 7 || input.FactSemanticHashes.Any(hash => hash.Length != 64))
            throw new AnnexE1SafeException(AnnexE1Outcome.MissingAccountingFact, "Every required Annex E-1 accounting fact or known-zero attestation must be authoritative.");
    }

    private static AnnexE1PositionValue Money(string position, string name, long value) => new(position, name, null, value, null);
    private static AnnexE1PositionValue Integer(string position, string name, long value) => new(position, name, null, null, value);
    private static AnnexE1PositionValue Text(string position, string name, string? value) => new(position, name, value, null, null);
    private static AnnexE1Reconciliation Rule(string name, long difference) => new(name, difference, difference == 0);
}
