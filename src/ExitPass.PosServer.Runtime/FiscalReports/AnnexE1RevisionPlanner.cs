namespace ExitPass.PosServer.Runtime.FiscalReports;

public enum AnnexE1RevisionDecision
{
    Replay,
    AutomaticSuccessor,
    ExplicitCorrectionRequired
}

public static class AnnexE1RevisionPlanner
{
    public static AnnexE1RevisionDecision Decide(
        AnnexE1WorkbookRecord current,
        string candidateSemanticHash,
        IReadOnlyList<AnnexE1Row> candidateRows)
    {
        if (string.Equals(current.SemanticHash, candidateSemanticHash, StringComparison.Ordinal) ||
            RowsEqual(current.Rows, candidateRows))
            return AnnexE1RevisionDecision.Replay;

        return candidateRows.Count > current.Rows.Count &&
               current.Rows.Zip(candidateRows).All(pair => RowEqual(pair.First, pair.Second))
            ? AnnexE1RevisionDecision.AutomaticSuccessor
            : AnnexE1RevisionDecision.ExplicitCorrectionRequired;
    }

    private static bool RowsEqual(IReadOnlyList<AnnexE1Row> left, IReadOnlyList<AnnexE1Row> right) =>
        left.Count == right.Count && left.Zip(right).All(pair => RowEqual(pair.First, pair.Second));

    private static bool RowEqual(AnnexE1Row left, AnnexE1Row right) =>
        left.Source.FiscalReportingPeriodId == right.Source.FiscalReportingPeriodId &&
        left.Source.GoverningZReportId == right.Source.GoverningZReportId &&
        left.Source.BirSalesSummaryReportId == right.Source.BirSalesSummaryReportId &&
        left.Positions.Count == right.Positions.Count &&
        left.Positions.Zip(right.Positions).All(pair =>
            pair.First.Position == pair.Second.Position &&
            pair.First.TextValue == pair.Second.TextValue &&
            pair.First.MinorUnitsValue == pair.Second.MinorUnitsValue &&
            pair.First.IntegerValue == pair.Second.IntegerValue);
}
