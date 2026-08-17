using System.Text;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.FiscalReports;

public sealed class AnnexE1V17WorkbookGenerationTests
{
    [Fact]
    public void GeneratesReadsBackAndReproducesAllInternalWorkbooks()
    {
        var root = Path.Combine(Path.GetTempPath(), "exitpass-annex-e1-v17-workbook-positive-" + Guid.NewGuid().ToString("N"));
        var datasetPath = DatasetPath();
        var outputPath = OptionalOutputPath("ANNEX_E1_V17_WORKBOOK_OUTPUT_PATH", Path.Combine(root, "published"));
        var reportPath = OptionalOutputPath("ANNEX_E1_V17_WORKBOOK_REPORT_PATH", Path.Combine(root, "report.txt"));
        try
        {
            Directory.CreateDirectory(root);
            var dataset = AnnexE1V17WorkbookGenerator.LoadDataset(datasetPath);
            var first = AnnexE1V17WorkbookGenerator.Generate(dataset, Path.Combine(root, "run-1"));
            var second = AnnexE1V17WorkbookGenerator.Generate(dataset, Path.Combine(root, "run-2"));
            AnnexE1V17WorkbookGenerator.CompareGenerations(first, second);

            Directory.CreateDirectory(outputPath);
            foreach (var file in Directory.GetFiles(first.Directory).OrderBy(Path.GetFileName, StringComparer.Ordinal))
                File.Copy(file, Path.Combine(outputPath, Path.GetFileName(file)), overwrite: false);

            var report = BuildReport(first);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            File.WriteAllText(reportPath, report, new UTF8Encoding(false));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public void RejectsAllRequiredWorkbookAndInputCorruptionsAndCleansFailureResources()
    {
        var datasetPath = DatasetPath();
        var root = Path.Combine(Path.GetTempPath(), "exitpass-annex-e1-v17-workbook-negative-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var dataset = AnnexE1V17WorkbookGenerator.LoadDataset(datasetPath);
            var cases = AnnexE1V17WorkbookGenerator.BuildCases(dataset);
            var generated = AnnexE1V17WorkbookGenerator.Generate(dataset, Path.Combine(root, "valid"));
            var firstCase = cases[0];
            var sourceWorkbook = Path.Combine(generated.Directory, generated.Entries[0].FileName);

            var corruptDataset = Path.Combine(root, "corrupt.jsonl");
            var bytes = File.ReadAllBytes(datasetPath);
            bytes[1024] ^= 0x01;
            File.WriteAllBytes(corruptDataset, bytes);
            Reject("corrupt input JSONL", () => AnnexE1V17WorkbookGenerator.LoadDataset(corruptDataset));

            Reject("omitted included case", () => AnnexE1V17WorkbookGenerator.ValidateDatasetShape(
                AnnexE1V17WorkbookGenerator.WithCases(dataset, dataset.CasePackages.Skip(1).ToArray())));
            Reject("added excluded case", () => AnnexE1V17WorkbookGenerator.ValidateDatasetShape(
                AnnexE1V17WorkbookGenerator.WithIncluded(dataset, dataset.Included.Concat(["002"]).ToArray())));
            Reject("omitted Annex row", () => AnnexE1V17WorkbookGenerator.ValidateDatasetShape(
                AnnexE1V17WorkbookGenerator.WithSemanticRows(dataset, dataset.SemanticRows.Where(x => x.Key != "F20|001|0001").ToArray())));

            RewriteAndReject(sourceWorkbook, Path.Combine(root, "bad-header.xlsx"), firstCase, xml =>
                ReplaceRequired(xml, "SYNTHETIC ANNEX E1 PARKING SERVICES INC.", "ALTERED SYNTHETIC TAXPAYER"), "changed header value");
            RewriteAndReject(sourceWorkbook, Path.Combine(root, "bad-detail.xlsx"), firstCase, xml =>
                ReplaceRequired(xml, "<c r=\"V9\" s=\"5\"><v>41200</v></c>", "<c r=\"V9\" s=\"5\"><v>41201</v></c>"), "changed D01-D32 value");
            RewriteAndReject(sourceWorkbook, Path.Combine(root, "bad-reconciliation.xlsx"), firstCase, xml =>
                ReplaceRequired(xml, "<c r=\"AV9\" s=\"5\"><v>30000</v></c>", "<c r=\"AV9\" s=\"5\"><v>29999</v></c>"), "changed R01-R12 value");
            RewriteAndReject(sourceWorkbook, Path.Combine(root, "wrong-case.xlsx"), firstCase, xml =>
                ReplaceRequired(xml, "<c r=\"B2\" s=\"5\" t=\"inlineStr\"><is><t xml:space=\"preserve\">DS-AE1-001</t></is></c>", "<c r=\"B2\" s=\"5\" t=\"inlineStr\"><is><t xml:space=\"preserve\">DS-AE1-003</t></is></c>"), "workbook assigned to wrong case");
            RewriteAndReject(sourceWorkbook, Path.Combine(root, "external-formula.xlsx"), firstCase, xml =>
                ReplaceRequired(xml, "</sheetData>", "<row r=\"99\"><c r=\"A99\"><f>NOW()</f><v>0</v></c></row></sheetData>"), "external link or volatile formula");

            var corruptWorkbook = Path.Combine(root, "corrupt-workbook.xlsx");
            var workbookBytes = File.ReadAllBytes(sourceWorkbook);
            File.WriteAllBytes(corruptWorkbook, workbookBytes[..^17]);
            Reject("corrupted generated workbook byte stream", () => AnnexE1V17WorkbookGenerator.ValidateWorkbook(corruptWorkbook, firstCase));

            var duplicateDirectory = Path.Combine(root, "duplicate");
            CopyDirectory(generated.Directory, duplicateDirectory);
            File.Copy(sourceWorkbook, Path.Combine(duplicateDirectory, "duplicate.xlsx"));
            Reject("duplicate workbook", () => AnnexE1V17WorkbookGenerator.ValidateWorkbookSet(duplicateDirectory, generated.Entries, cases));

            var failureDirectory = Path.Combine(root, "failure-cleanup");
            try
            {
                Directory.CreateDirectory(failureDirectory);
                File.WriteAllText(Path.Combine(failureDirectory, "owned.tmp"), "owned", new UTF8Encoding(false));
                throw new InvalidOperationException("INTENTIONAL_WORKBOOK_FAILURE_PATH");
            }
            catch (InvalidOperationException exception) when (exception.Message == "INTENTIONAL_WORKBOOK_FAILURE_PATH")
            {
                // The finally block is the behavior under test.
            }
            finally
            {
                if (Directory.Exists(failureDirectory)) Directory.Delete(failureDirectory, recursive: true);
            }
            Assert.False(Directory.Exists(failureDirectory));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
        Assert.False(Directory.Exists(root));
    }

    private static string BuildReport(AnnexE1V17WorkbookGenerator.GenerationResult result)
    {
        var builder = new StringBuilder();
        builder.Append("ANNEX_E1_V17_INTERNAL_WORKBOOK_GENERATION=PASS\n");
        builder.Append("WORKBOOKS=").Append(result.Entries.Count).Append('\n');
        builder.Append("ANNEX_ROWS=").Append(result.Entries.Sum(x => x.AnnexRowCount)).Append('\n');
        builder.Append("AGGREGATE_MANIFEST_SHA256=").Append(result.AggregateManifestSha256).Append('\n');
        builder.Append("MANIFEST_FILE_SHA256=").Append(result.ManifestFileSha256).Append('\n');
        foreach (var entry in result.Entries.OrderBy(x => x.CaseId, StringComparer.Ordinal))
        {
            builder.Append("WORKBOOK=").Append(entry.CaseId).Append('|').Append(entry.WorkbookId).Append('|').Append(entry.AnnexRowCount).Append('|')
                .Append(entry.ByteLength).Append('|').Append(entry.WorkbookSha256).Append('|').Append(entry.NormalizedSemanticContentSha256).Append('|')
                .Append(entry.AuthorizedSemanticCommitmentSha256).Append('\n');
        }
        return builder.ToString();
    }

    private static void RewriteAndReject(string source, string target, AnnexE1V17WorkbookGenerator.WorkbookCase expected, Func<string, string> transform, string name)
    {
        AnnexE1V17WorkbookGenerator.RewriteEntry(source, target, "xl/worksheets/sheet2.xml", transform);
        Reject(name, () => AnnexE1V17WorkbookGenerator.ValidateWorkbook(target, expected));
    }

    private static string ReplaceRequired(string source, string oldValue, string newValue)
    {
        Assert.Contains(oldValue, source, StringComparison.Ordinal);
        return source.Replace(oldValue, newValue, StringComparison.Ordinal);
    }

    private static void Reject(string name, Action action)
    {
        var exception = Record.Exception(action);
        Assert.NotNull(exception);
        Assert.True(exception is InvalidDataException or InvalidOperationException, $"Negative test '{name}' failed with unexpected {exception.GetType().Name}: {exception.Message}");
    }

    private static string DatasetPath()
    {
        const string relative = "docs/v1.3/fiscal-reporting/annex-e/dataset/v1.7/annex-e1-synthetic-uat-dataset-v1.7.jsonl";
        var configured = Environment.GetEnvironmentVariable("ANNEX_E1_V17_WORKBOOK_DATASET_PATH");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            Assert.True(File.Exists(configured), $"Configured dataset does not exist: {configured}");
            return Path.GetFullPath(configured);
        }
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate)) return candidate;
        }
        throw new InvalidOperationException("Could not locate the committed Annex E-1 v1.7 dataset.");
    }

    private static string OptionalOutputPath(string name, string fallback)
    {
        var configured = Environment.GetEnvironmentVariable(name);
        var path = Path.GetFullPath(string.IsNullOrWhiteSpace(configured) ? fallback : configured);
        Assert.False(File.Exists(path), $"{name} must identify an output path, not a file: {path}");
        return path;
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
    }
}
