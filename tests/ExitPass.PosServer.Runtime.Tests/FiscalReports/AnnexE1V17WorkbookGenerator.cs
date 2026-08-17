using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Runtime.Tests.FiscalReports;

internal static class AnnexE1V17WorkbookGenerator
{
    internal const string DatasetSha256 = "ed6971df7a142a54492eea968587a150c0229618650cee56ea2116154692a1ac";
    internal const long DatasetBytes = 8_070_439;
    internal const string PackageRootSha256 = "5d60cb50d7f43b29f79486801a470e57128e2b571c4e50a37494e4a82131b829";
    internal const string InternalMark = "SYNTHETIC / INTERNAL TEST ONLY / NOT FOR BIR SUBMISSION";
    internal static readonly string[] IncludedScenarios = ["001", "003", "004", "005", "007", "009", "010", "011", "012", "013", "014", "016", "017", "018", "019", "020", "021", "023", "024"];
    internal static readonly string[] ExcludedScenarios = ["002", "006", "008", "015", "022", "025"];

    private static readonly DateTime FixedZipWallTime = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTimeOffset FixedZipTime = new(FixedZipWallTime, TimeZoneInfo.Local.GetUtcOffset(FixedZipWallTime));
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    internal sealed record SemanticRow(
        string Key,
        string Family,
        string InstanceUuid,
        string Scenario,
        int Ordinal,
        IReadOnlyList<Member> Members,
        string ExpectedSemanticSha256,
        string OriginalJson);

    internal sealed record Member(string Name, string Type, JsonElement Value);

    internal sealed record Dataset(
        IReadOnlyList<string> Included,
        IReadOnlyList<string> Excluded,
        IReadOnlyList<SemanticRow> SemanticRows,
        IReadOnlyList<JsonElement> CasePackages,
        IReadOnlyList<JsonElement> Identities);

    internal sealed record WorkbookCase(
        string Scenario,
        string CaseId,
        string WorkbookId,
        string AuthorizedContentSha256,
        IReadOnlyList<SemanticRow> AnnexRows,
        IReadOnlyList<SemanticRow> FactLinks,
        SemanticRow WorkbookRow,
        string CasePackageSha256);

    internal sealed record ManifestEntry(
        string CaseId,
        string Scenario,
        string WorkbookId,
        string FileName,
        int AnnexRowCount,
        long ByteLength,
        string WorkbookSha256,
        string NormalizedSemanticContentSha256,
        string AuthorizedSemanticCommitmentSha256);

    internal sealed record GenerationResult(
        string Directory,
        IReadOnlyList<ManifestEntry> Entries,
        byte[] ManifestBytes,
        string AggregateManifestSha256,
        string ManifestFileSha256);

    internal static Dataset LoadDataset(string path, bool requireCommittedHash = true)
    {
        var workingBytes = File.ReadAllBytes(path);
        var workingText = new UTF8Encoding(false, true).GetString(workingBytes);
        var canonicalText = workingText.Replace("\r\n", "\n", StringComparison.Ordinal);
        Require(!canonicalText.Contains('\r'), "Dataset contains a non-CRLF carriage return.");
        var bytes = Utf8NoBom.GetBytes(canonicalText);
        if (requireCommittedHash)
        {
            Require(bytes.LongLength == DatasetBytes, $"Dataset byte length mismatch: expected {DatasetBytes}, actual {bytes.LongLength}.");
            Require(Sha256(bytes) == DatasetSha256, "Dataset SHA-256 does not match the committed v1.7 artifact.");
        }

        Require(bytes.Length > 0 && bytes[^1] == (byte)'\n', "Dataset must end with LF.");
        var lines = canonicalText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var semantic = new List<SemanticRow>();
        var cases = new List<JsonElement>();
        var identities = new List<JsonElement>();
        string[]? included = null;
        string[]? excluded = null;

        foreach (var line in lines)
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            var recordType = root.GetProperty("recordType").GetString();
            switch (recordType)
            {
                case "dataset-metadata":
                    Require(root.GetProperty("sourceSpecificationPackageRootSha256").GetString() == PackageRootSha256, "Dataset package-root commitment mismatch.");
                    included = root.GetProperty("includedScenarios").EnumerateArray().Select(x => x.GetString()!).ToArray();
                    excluded = root.GetProperty("excludedScenarios").EnumerateArray().Select(x => x.GetString()!).ToArray();
                    break;
                case "identity":
                    identities.Add(root.Clone());
                    break;
                case "semantic-instance":
                    semantic.Add(ParseSemantic(root, line));
                    break;
                case "case-package":
                    cases.Add(root.Clone());
                    break;
                default:
                    throw new InvalidDataException($"Unknown dataset record type '{recordType}'.");
            }
        }

        Require(included is not null && excluded is not null, "Dataset metadata is missing.");
        var dataset = new Dataset(included!, excluded!, semantic, cases, identities);
        ValidateDatasetShape(dataset);
        return dataset;
    }

    internal static Dataset WithCases(Dataset source, IReadOnlyList<JsonElement> cases) => source with { CasePackages = cases };
    internal static Dataset WithSemanticRows(Dataset source, IReadOnlyList<SemanticRow> rows) => source with { SemanticRows = rows };
    internal static Dataset WithIncluded(Dataset source, IReadOnlyList<string> included) => source with { Included = included };

    internal static void ValidateDatasetShape(Dataset dataset)
    {
        Require(dataset.Included.SequenceEqual(IncludedScenarios, StringComparer.Ordinal), "Included scenario partition mismatch.");
        Require(dataset.Excluded.SequenceEqual(ExcludedScenarios, StringComparer.Ordinal), "Excluded scenario partition mismatch.");
        Require(dataset.Included.Intersect(dataset.Excluded, StringComparer.Ordinal).Any() is false, "Included and excluded scenarios overlap.");
        Require(dataset.Identities.Count == 2_364, $"Identity count mismatch: {dataset.Identities.Count}.");
        Require(dataset.SemanticRows.Count == 1_690, $"Semantic-instance count mismatch: {dataset.SemanticRows.Count}.");
        Require(dataset.CasePackages.Count == 19, $"Case-package count mismatch: {dataset.CasePackages.Count}.");
        Require(dataset.SemanticRows.Select(x => x.Key).Distinct(StringComparer.Ordinal).Count() == 1_690, "Duplicate semantic-instance key.");
        Require(dataset.Identities.Select(x => x.GetProperty("uuid").GetString()).Distinct(StringComparer.Ordinal).Count() == 2_364, "Duplicate deterministic identity.");

        var caseScenarios = dataset.CasePackages.Select(x => x.GetProperty("scenario").GetString()!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Require(caseScenarios.SequenceEqual(IncludedScenarios, StringComparer.Ordinal), "Case-package scenario set mismatch.");
        Require(caseScenarios.Intersect(ExcludedScenarios, StringComparer.Ordinal).Any() is false, "Excluded scenario has a case package.");
        Require(dataset.SemanticRows.Count(x => x.Family == "F20") == 22, "Annex-row count must be 22.");
        Require(dataset.SemanticRows.Count(x => x.Family == "F21") == 154, "Fact-link count must be 154.");
        Require(dataset.SemanticRows.Count(x => x.Family == "F24") == 19, "Workbook-row count must be 19.");
    }

    internal static IReadOnlyList<WorkbookCase> BuildCases(Dataset dataset)
    {
        ValidateDatasetShape(dataset);
        var result = new List<WorkbookCase>(19);
        foreach (var scenario in IncludedScenarios)
        {
            var package = dataset.CasePackages.Single(x => x.GetProperty("scenario").GetString() == scenario);
            var caseId = package.GetProperty("caseId").GetString()!;
            var workbook = dataset.SemanticRows.Single(x => x.Family == "F24" && x.Scenario == scenario);
            var workbookId = Value(workbook, "id").GetString()!;
            var rows = dataset.SemanticRows.Where(x => x.Family == "F20" && x.Scenario == scenario).OrderBy(x => x.Ordinal).ToArray();
            var links = dataset.SemanticRows.Where(x => x.Family == "F21" && x.Scenario == scenario).OrderBy(x => x.Ordinal).ToArray();
            Require(rows.Length > 0, $"Scenario {scenario} has no Annex row.");
            Require(rows.Select((x, index) => Value(x, "row_sequence").GetInt64() == index + 1).All(x => x), $"Scenario {scenario} Annex row ordering is not gap-free.");
            Require(rows.Select(x => ObjectMembers(x, "details")["D01"].Value.GetString()).SequenceEqual(rows.Select(x => ObjectMembers(x, "details")["D01"].Value.GetString()).OrderBy(x => x, StringComparer.Ordinal)), $"Scenario {scenario} period ordering is unstable.");
            Require(links.Select((x, index) => x.Ordinal == index + 1).All(x => x), $"Scenario {scenario} fact-link semantic ordering is not gap-free.");
            Require(rows.All(x => Value(x, "workbook_id").GetString() == workbookId), $"Scenario {scenario} has an Annex row assigned to another workbook.");
            Require(links.All(x => Value(x, "workbook_id").GetString() == workbookId), $"Scenario {scenario} has a fact link assigned to another workbook.");
            result.Add(new WorkbookCase(
                scenario,
                caseId,
                workbookId,
                Value(workbook, "workbook_content_sha256").GetString()!,
                rows,
                links,
                workbook,
                package.GetProperty("sha256").GetString()!));
        }
        Require(result.Sum(x => x.AnnexRows.Count) == 22, "Generated case topology must contain 22 Annex rows.");
        return result;
    }

    internal static GenerationResult Generate(Dataset dataset, string outputDirectory)
    {
        Require(!Directory.Exists(outputDirectory), $"Output directory already exists: {outputDirectory}");
        Directory.CreateDirectory(outputDirectory);
        var entries = new List<ManifestEntry>();
        foreach (var item in BuildCases(dataset))
        {
            var fileName = $"{item.CaseId}.synthetic-internal-annex-e1.xlsx";
            var path = Path.Combine(outputDirectory, fileName);
            var bytes = RenderWorkbook(item);
            File.WriteAllBytes(path, bytes);
            ValidateWorkbook(path, item);
            entries.Add(new ManifestEntry(
                item.CaseId,
                item.Scenario,
                item.WorkbookId,
                fileName,
                item.AnnexRows.Count,
                bytes.LongLength,
                Sha256(bytes),
                NormalizedSemanticHash(item),
                item.AuthorizedContentSha256));
        }

        ValidateWorkbookSet(outputDirectory, entries, BuildCases(dataset));
        var aggregate = AggregateManifestHash(entries);
        var manifestBytes = BuildManifest(entries, aggregate);
        var manifestPath = Path.Combine(outputDirectory, "annex-e1-v1.7-internal-workbook-manifest.json");
        File.WriteAllBytes(manifestPath, manifestBytes);
        return new GenerationResult(outputDirectory, entries, manifestBytes, aggregate, Sha256(manifestBytes));
    }

    internal static void ValidateWorkbookSet(string directory, IReadOnlyList<ManifestEntry> entries, IReadOnlyList<WorkbookCase> cases)
    {
        var files = Directory.GetFiles(directory, "*.xlsx", SearchOption.TopDirectoryOnly).OrderBy(Path.GetFileName, StringComparer.Ordinal).ToArray();
        Require(files.Length == 19, $"Workbook file count mismatch: {files.Length}.");
        Require(entries.Count == 19, $"Manifest workbook count mismatch: {entries.Count}.");
        Require(entries.Sum(x => x.AnnexRowCount) == 22, "Manifest Annex-row count mismatch.");
        Require(entries.Select(x => x.CaseId).Distinct(StringComparer.Ordinal).Count() == 19, "Duplicate case in workbook manifest.");
        Require(entries.Select(x => x.WorkbookId).Distinct(StringComparer.Ordinal).Count() == 19, "Duplicate workbook identity in workbook manifest.");
        Require(entries.Select(x => x.Scenario).Intersect(ExcludedScenarios, StringComparer.Ordinal).Any() is false, "Excluded scenario appears in workbook manifest.");
        foreach (var item in cases)
        {
            var entry = entries.Single(x => x.CaseId == item.CaseId);
            var path = Path.Combine(directory, entry.FileName);
            Require(File.Exists(path), $"Workbook is missing: {entry.FileName}");
            var bytes = File.ReadAllBytes(path);
            Require(bytes.LongLength == entry.ByteLength, $"Workbook length mismatch: {entry.FileName}");
            Require(Sha256(bytes) == entry.WorkbookSha256, $"Workbook digest mismatch: {entry.FileName}");
            ValidateWorkbook(path, item);
        }
    }

    internal static byte[] RenderWorkbook(WorkbookCase item)
    {
        var first = item.AnnexRows[0];
        var headers = ObjectMembers(first, "headers");
        foreach (var row in item.AnnexRows.Skip(1))
        {
            var other = ObjectMembers(row, "headers");
            Require(headers.All(x => other[x.Key].Value.GetString() == x.Value.Value.GetString()), $"Scenario {item.Scenario} contains inconsistent headers.");
        }

        var h04 = headers["H04"].Value.GetString()!;
        var h05 = headers["H05"].Value.GetString()!;
        Require(h04 == "ExitPass POS Server 1.3", $"Unexpected H04 for {item.CaseId}.");
        Require(h05 == "Z-012B / 2026-08-10", $"Unexpected H05 for {item.CaseId}.");
        var generatedAt = DateTimeOffset.ParseExact(headers["H09"].Value.GetString()!, "yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        var header = new AnnexE1Header(
            headers["H01"].Value.GetString()!, headers["H02"].Value.GetString()!, headers["H03"].Value.GetString()!,
            "ExitPass POS Server", "1.3", "Z-012B", "2026-08-10",
            headers["H06"].Value.GetString()!, headers["H07"].Value.GetString()!, headers["H08"].Value.GetString()!, generatedAt, headers["H10"].Value.GetString()!);
        var renderedRows = item.AnnexRows.Select(ToRuntimeRow).ToArray();
        var baseBytes = new AnnexE1DeterministicXlsxRenderer().Render(header, renderedRows);
        return AddValidationParts(baseBytes, item);
    }

    internal static void ValidateWorkbook(string path, WorkbookCase expected)
    {
        AnnexE1OpenXmlStandardsValidator.Validate(path);
        using var stream = File.OpenRead(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: Encoding.UTF8);
        var names = archive.Entries.Select(x => x.FullName).ToArray();
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "[Content_Types].xml", "_rels/.rels", "docProps/app.xml", "docProps/core.xml", "xl/workbook.xml",
            "xl/_rels/workbook.xml.rels", "xl/styles.xml", "xl/worksheets/sheet1.xml", "xl/worksheets/_rels/sheet1.xml.rels",
            "xl/worksheets/sheet2.xml", "xl/worksheets/sheet3.xml", "xl/drawings/drawing1.xml"
        };
        Require(names.Length == allowed.Count && names.All(allowed.Contains), "Workbook contains an unexpected, missing, or duplicate ZIP member.");
        Require(archive.Entries.All(x => x.LastWriteTime.DateTime == FixedZipWallTime), "Workbook contains volatile ZIP timestamps.");
        foreach (var entry in archive.Entries.Where(x => x.FullName.EndsWith(".xml", StringComparison.Ordinal) || x.FullName.EndsWith(".rels", StringComparison.Ordinal)))
        {
            var xml = ReadEntry(entry);
            var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            Require(!document.Descendants().Any(x => x.Name.LocalName == "f"), "Workbook contains a formula.");
            Require(!document.Descendants().Any(x => string.Equals((string?)x.Attribute("TargetMode"), "External", StringComparison.OrdinalIgnoreCase)), "Workbook contains an external relationship.");
            Require(!xml.Contains("externalLink", StringComparison.OrdinalIgnoreCase), "Workbook contains an external link.");
            Require(!xml.Contains("connection", StringComparison.OrdinalIgnoreCase), "Workbook contains a data connection.");
            Require(!xml.Contains("vbaProject", StringComparison.OrdinalIgnoreCase), "Workbook contains a macro payload.");
            Require(!xml.Contains("NOW()", StringComparison.OrdinalIgnoreCase) && !xml.Contains("RAND()", StringComparison.OrdinalIgnoreCase) && !xml.Contains("TODAY()", StringComparison.OrdinalIgnoreCase), "Workbook contains a volatile expression.");
        }

        ValidateWorkbookMetadata(archive, expected);
        ValidateMainSheet(archive, expected);
        ValidateValidationSheet(archive, expected);
        ValidateFactLinkSheet(archive, expected);
        ValidateCellTypes(archive, expected);
    }

    internal static string NormalizedSemanticHash(WorkbookCase item)
    {
        var builder = new StringBuilder();
        builder.Append("ANNEX_E1_INTERNAL_WORKBOOK_SEMANTIC_CONTENT=v1\n");
        builder.Append("case=").Append(item.CaseId).Append('\n');
        builder.Append("scenario=").Append(item.Scenario).Append('\n');
        builder.Append("workbook=").Append(item.WorkbookId).Append('\n');
        builder.Append("authorized_workbook_content_sha256=").Append(item.AuthorizedContentSha256).Append('\n');
        builder.Append("case_package_sha256=").Append(item.CasePackageSha256).Append('\n');
        foreach (var row in item.AnnexRows.OrderBy(x => x.Key, StringComparer.Ordinal))
            builder.Append("annex=").Append(row.Key).Append('|').Append(row.ExpectedSemanticSha256).Append('\n');
        foreach (var row in item.FactLinks.OrderBy(x => x.Key, StringComparer.Ordinal))
            builder.Append("fact_link=").Append(row.Key).Append('|').Append(row.ExpectedSemanticSha256).Append('\n');
        return Sha256(Utf8NoBom.GetBytes(builder.ToString()));
    }

    internal static void CompareGenerations(GenerationResult first, GenerationResult second)
    {
        Require(first.AggregateManifestSha256 == second.AggregateManifestSha256, "Aggregate manifest digest is not deterministic.");
        Require(first.ManifestFileSha256 == second.ManifestFileSha256, "Manifest file digest is not deterministic.");
        Require(first.ManifestBytes.SequenceEqual(second.ManifestBytes), "Manifest bytes are not deterministic.");
        Require(first.Entries.Count == second.Entries.Count, "Workbook manifest cardinality changed between runs.");
        foreach (var left in first.Entries)
        {
            var right = second.Entries.Single(x => x.CaseId == left.CaseId);
            Require(left == right, $"Workbook manifest entry changed between runs: {left.CaseId}");
            Require(File.ReadAllBytes(Path.Combine(first.Directory, left.FileName)).SequenceEqual(File.ReadAllBytes(Path.Combine(second.Directory, right.FileName))), $"Workbook bytes changed between runs: {left.CaseId}");
        }
    }

    internal static void RewriteEntry(string sourcePath, string targetPath, string entryName, Func<string, string> transform)
    {
        using var inputStream = File.OpenRead(sourcePath);
        using var input = new ZipArchive(inputStream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: Encoding.UTF8);
        var entries = input.Entries.Select(x => (x.FullName, Bytes: ReadEntryBytes(x))).ToArray();
        using var outputStream = File.Create(targetPath);
        using var output = new ZipArchive(outputStream, ZipArchiveMode.Create, leaveOpen: false, entryNameEncoding: Encoding.UTF8);
        foreach (var item in entries)
        {
            var bytes = item.FullName == entryName ? Utf8NoBom.GetBytes(transform(Utf8NoBom.GetString(item.Bytes))) : item.Bytes;
            AddEntry(output, item.FullName, bytes);
        }
    }

    private static SemanticRow ParseSemantic(JsonElement root, string originalJson)
    {
        var members = root.GetProperty("members").EnumerateArray()
            .Select(x => new Member(x.GetProperty("n").GetString()!, x.GetProperty("t").GetString()!, x.GetProperty("v").Clone()))
            .ToArray();
        return new SemanticRow(
            root.GetProperty("key").GetString()!, root.GetProperty("family").GetString()!, root.GetProperty("instanceUuid").GetString()!,
            root.GetProperty("scenario").GetString()!, root.GetProperty("ordinal").GetInt32(), members,
            root.GetProperty("expectedSemanticSha256").GetString()!, originalJson);
    }

    private static AnnexE1Row ToRuntimeRow(SemanticRow row)
    {
        var details = ObjectMembers(row, "details");
        var positions = new List<AnnexE1PositionValue>(32);
        for (var index = 1; index <= 32; index++)
        {
            var name = $"D{index:00}";
            var member = details[name];
            if (index is <= 3 or 32)
                positions.Add(new AnnexE1PositionValue(name, name, member.Value.GetString(), null, null));
            else if (index is 30 or 31)
                positions.Add(new AnnexE1PositionValue(name, name, null, null, member.Value.GetInt64()));
            else
                positions.Add(new AnnexE1PositionValue(name, name, null, member.Value.GetInt64(), null));
        }
        var reconciliations = Reconciliations(row).Select(x => new AnnexE1Reconciliation(x.Rule, x.DifferenceMinorUnits, x.Result == "PASS")).ToArray();
        return new AnnexE1Row(null!, positions, reconciliations);
    }

    private static byte[] AddValidationParts(byte[] baseBytes, WorkbookCase item)
    {
        using var inputStream = new MemoryStream(baseBytes);
        using var input = new ZipArchive(inputStream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: Encoding.UTF8);
        var parts = input.Entries.ToDictionary(x => x.FullName, ReadEntryBytes, StringComparer.Ordinal);
        parts["[Content_Types].xml"] = ReplaceUtf8(parts["[Content_Types].xml"], "</Types>", "<Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet3.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
        // Excel rejects the runtime renderer's incomplete optional fileVersion element with 0x800A03EC.
        parts["xl/workbook.xml"] = ReplaceUtf8(parts["xl/workbook.xml"], "<fileVersion appName=\"xl\"/>", string.Empty);
        parts["xl/workbook.xml"] = ReplaceUtf8(parts["xl/workbook.xml"], "</sheets>", "<sheet name=\"Validation\" sheetId=\"2\" r:id=\"rId3\"/><sheet name=\"Fact Links\" sheetId=\"3\" r:id=\"rId4\"/></sheets>");
        parts["xl/worksheets/sheet1.xml"] = ReplaceUtf8(parts["xl/worksheets/sheet1.xml"], "<row r=\"11\"/>", $"<row r=\"11\" ht=\"21.75\"><c r=\"A11\" s=\"1\" t=\"inlineStr\"><is><t>{Escape(InternalMark)}</t></is></c></row>");
        parts["xl/worksheets/sheet1.xml"] = ReplaceUtf8(parts["xl/worksheets/sheet1.xml"], "<mergeCells count=\"32\">", "<mergeCells count=\"33\"><mergeCell ref=\"A11:AF11\"/>");
        parts["xl/_rels/workbook.xml.rels"] = ReplaceUtf8(parts["xl/_rels/workbook.xml.rels"], "</Relationships>", "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/><Relationship Id=\"rId4\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet3.xml\"/></Relationships>");
        parts["xl/worksheets/sheet2.xml"] = Utf8NoBom.GetBytes(ValidationWorksheet(item));
        parts["xl/worksheets/sheet3.xml"] = Utf8NoBom.GetBytes(FactLinkWorksheet(item));

        string[] order = [
            "[Content_Types].xml", "_rels/.rels", "docProps/app.xml", "docProps/core.xml", "xl/workbook.xml",
            "xl/_rels/workbook.xml.rels", "xl/styles.xml", "xl/worksheets/sheet1.xml", "xl/worksheets/_rels/sheet1.xml.rels",
            "xl/worksheets/sheet2.xml", "xl/worksheets/sheet3.xml", "xl/drawings/drawing1.xml"
        ];
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: Encoding.UTF8))
        {
            foreach (var name in order) AddEntry(archive, name, parts[name]);
        }
        return output.ToArray();
    }

    private static string ValidationWorksheet(WorkbookCase item)
    {
        var columns = new List<string> { "annex_row_id", "row_sequence", "source_semantic_hash", "calculation_semantic_hash", "status" };
        columns.AddRange(Enumerable.Range(1, 10).Select(x => $"H{x:00}"));
        columns.AddRange(Enumerable.Range(1, 32).Select(x => $"D{x:00}"));
        foreach (var rule in Enumerable.Range(1, 12).Select(x => $"R{x:00}"))
        {
            columns.AddRange([ $"{rule}_expected_minor_units", $"{rule}_calculated_minor_units", $"{rule}_difference_minor_units", $"{rule}_expected_row_count", $"{rule}_calculated_row_count", $"{rule}_row_count_difference", $"{rule}_result" ]);
        }

        var rows = new List<IReadOnlyList<CellValue>>
        {
            new CellValue[] { CellValue.Text(InternalMark) },
            new CellValue[] { CellValue.Text("case_id"), CellValue.Text(item.CaseId) },
            new CellValue[] { CellValue.Text("scenario_id"), CellValue.Text(item.Scenario) },
            new CellValue[] { CellValue.Text("workbook_id"), CellValue.Text(item.WorkbookId) },
            new CellValue[] { CellValue.Text("authorized_semantic_commitment_sha256"), CellValue.Text(item.AuthorizedContentSha256) },
            new CellValue[] { CellValue.Text("case_package_sha256"), CellValue.Text(item.CasePackageSha256) },
            new CellValue[] { CellValue.Text("normalized_semantic_content_sha256"), CellValue.Text(NormalizedSemanticHash(item)) },
            columns.Select(CellValue.Text).ToArray()
        };
        foreach (var annex in item.AnnexRows.OrderBy(x => x.Ordinal))
        {
            var values = new List<CellValue>
            {
                CellValue.Text(Value(annex, "id").GetString()!), CellValue.Integer(Value(annex, "row_sequence").GetInt64()),
                CellValue.Text(Value(annex, "source_semantic_hash").GetString()!), CellValue.Text(Value(annex, "calculation_semantic_hash").GetString()!),
                CellValue.Text(Value(annex, "status").GetString()!)
            };
            var headers = ObjectMembers(annex, "headers");
            values.AddRange(Enumerable.Range(1, 10).Select(index => CellValue.Text(headers[$"H{index:00}"].Value.GetString()!)));
            var details = ObjectMembers(annex, "details");
            foreach (var detail in Enumerable.Range(1, 32).Select(index => details[$"D{index:00}"]))
                values.Add(detail.Type is "int64" ? CellValue.Integer(detail.Value.GetInt64()) : CellValue.Text(detail.Value.GetString()!));
            foreach (var reconciliation in Reconciliations(annex))
            {
                values.AddRange([
                    CellValue.Integer(reconciliation.ExpectedMinorUnits), CellValue.Integer(reconciliation.CalculatedMinorUnits), CellValue.Integer(reconciliation.DifferenceMinorUnits),
                    CellValue.Integer(reconciliation.ExpectedRowCount), CellValue.Integer(reconciliation.CalculatedRowCount), CellValue.Integer(reconciliation.RowCountDifference), CellValue.Text(reconciliation.Result)
                ]);
            }
            Require(values.Count == columns.Count, $"Validation column cardinality mismatch for {annex.Key}.");
            rows.Add(values);
        }
        return Worksheet(rows);
    }

    private static string FactLinkWorksheet(WorkbookCase item)
    {
        var rows = new List<IReadOnlyList<CellValue>>
        {
            new CellValue[] { CellValue.Text(InternalMark) },
            new CellValue[] { CellValue.Text("id"), CellValue.Text("workbook_id"), CellValue.Text("period_id"), CellValue.Text("accounting_fact_id"), CellValue.Text("fact_type"), CellValue.Text("fact_semantic_hash"), CellValue.Text("source_ordinal"), CellValue.Text("created_at") }
        };
        foreach (var link in item.FactLinks.OrderBy(x => x.Ordinal))
        {
            rows.Add([
                CellValue.Text(Value(link, "id").GetString()!), CellValue.Text(Value(link, "workbook_id").GetString()!), CellValue.Text(Value(link, "period_id").GetString()!),
                CellValue.Text(Value(link, "accounting_fact_id").GetString()!), CellValue.Text(Value(link, "fact_type").GetString()!), CellValue.Text(Value(link, "fact_semantic_hash").GetString()!),
                CellValue.Integer(Value(link, "source_ordinal").GetInt64()), CellValue.Text(Value(link, "created_at").GetString()!)
            ]);
        }
        return Worksheet(rows);
    }

    private static string Worksheet(IReadOnlyList<IReadOnlyList<CellValue>> rows)
    {
        var maxColumn = rows.Max(x => x.Count);
        var builder = new StringBuilder(65536);
        builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><dimension ref=\"A1:")
            .Append(Column(maxColumn)).Append(rows.Count).Append("\"/><sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews><sheetFormatPr defaultRowHeight=\"15\"/><sheetData>");
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            builder.Append("<row r=\"").Append(rowIndex + 1).Append("\">");
            for (var columnIndex = 0; columnIndex < rows[rowIndex].Count; columnIndex++)
            {
                var cell = rows[rowIndex][columnIndex];
                var reference = Column(columnIndex + 1) + (rowIndex + 1).ToString(CultureInfo.InvariantCulture);
                if (cell.IntegerValue is long integer)
                    builder.Append("<c r=\"").Append(reference).Append("\" s=\"5\"><v>").Append(integer.ToString(CultureInfo.InvariantCulture)).Append("</v></c>");
                else
                    builder.Append("<c r=\"").Append(reference).Append("\" s=\"5\" t=\"inlineStr\"><is><t xml:space=\"preserve\">").Append(Escape(cell.TextValue ?? string.Empty)).Append("</t></is></c>");
            }
            builder.Append("</row>");
        }
        builder.Append("</sheetData><pageMargins left=\"0.25\" right=\"0.25\" top=\"0.5\" bottom=\"0.5\" header=\"0.3\" footer=\"0.3\"/></worksheet>");
        return builder.ToString();
    }

    private static void ValidateWorkbookMetadata(ZipArchive archive, WorkbookCase expected)
    {
        var workbook = XDocument.Parse(ReadEntry(archive.GetEntry("xl/workbook.xml")!));
        var ns = workbook.Root!.Name.Namespace;
        var sheets = workbook.Descendants(ns + "sheet").ToArray();
        Require(sheets.Select(x => (string?)x.Attribute("name")).SequenceEqual(["E-1", "Validation", "Fact Links"], StringComparer.Ordinal), "Workbook sheet set or order mismatch.");
        Require(sheets.All(x => x.Attribute("state") is null), "Workbook contains a hidden sheet.");
        Require(workbook.Descendants(ns + "fileVersion").Any() is false, "Workbook contains the Excel-incompatible incomplete fileVersion element.");
        var printArea = workbook.Descendants(ns + "definedName").SingleOrDefault(x => (string?)x.Attribute("name") == "_xlnm.Print_Area" && (string?)x.Attribute("localSheetId") == "0");
        Require(printArea is not null && printArea.Value.StartsWith("'E-1'!$A$1:", StringComparison.Ordinal), "E-1 print area is missing or excludes the internal-test warning row.");
        var core = ReadEntry(archive.GetEntry("docProps/core.xml")!);
        Require(core.Contains("2000-01-01T00:00:00Z", StringComparison.Ordinal), "Workbook core properties are not fixed.");
        Require(expected.WorkbookId.Length == 36, "Workbook identity is invalid.");
    }

    private static void ValidateMainSheet(ZipArchive archive, WorkbookCase expected)
    {
        var entry = archive.GetEntry("xl/worksheets/sheet1.xml")!;
        var document = XDocument.Parse(ReadEntry(entry));
        var ns = document.Root!.Name.Namespace;
        var cells = ReadCells(entry);
        var headers = ObjectMembers(expected.AnnexRows[0], "headers");
        EqualCell(cells, "A1", headers["H01"].Value.GetString()!);
        EqualCell(cells, "A2", headers["H02"].Value.GetString()!);
        EqualCell(cells, "A3", headers["H03"].Value.GetString()!);
        EqualCell(cells, "A5", headers["H04"].Value.GetString()! + " / " + headers["H05"].Value.GetString()!);
        EqualCell(cells, "A6", headers["H06"].Value.GetString()!);
        EqualCell(cells, "A7", headers["H07"].Value.GetString()!);
        EqualCell(cells, "A8", headers["H08"].Value.GetString()!);
        EqualCell(cells, "A9", headers["H09"].Value.GetString()!);
        EqualCell(cells, "A10", headers["H10"].Value.GetString()!);
        EqualCell(cells, "A11", InternalMark);
        Require(document.Descendants(ns + "t").Count(x => x.Value == InternalMark) == 1, "Printable E-1 sheet must contain the exact internal-test warning exactly once.");
        var warningRow = document.Descendants(ns + "row").Single(x => (string?)x.Attribute("r") == "11");
        Require((string?)warningRow.Attribute("hidden") is null or "0", "Printable E-1 internal-test warning row is hidden.");
        Require(document.Descendants(ns + "mergeCell").Any(x => (string?)x.Attribute("ref") == "A11:AF11"), "Printable E-1 warning is not visibly merged across the report width.");
        var annexRows = expected.AnnexRows.OrderBy(x => x.Ordinal).ToArray();
        for (var rowIndex = 0; rowIndex < annexRows.Length; rowIndex++)
        {
            var details = ObjectMembers(annexRows[rowIndex], "details");
            for (var index = 1; index <= 32; index++)
            {
                var name = $"D{index:00}";
                var actual = cells[Column(index) + (17 + rowIndex).ToString(CultureInfo.InvariantCulture)];
                if (index is <= 3 or 32) Require(actual == details[name].Value.GetString(), $"{expected.CaseId} {name} text mismatch.");
                else if (index is 30 or 31) Require(actual == details[name].Value.GetInt64().ToString(CultureInfo.InvariantCulture), $"{expected.CaseId} {name} integer mismatch.");
                else
                {
                    Require(actual.Contains('.') && actual.Length - actual.IndexOf('.') - 1 == 2, $"{expected.CaseId} {name} monetary precision mismatch.");
                    Require(decimal.Parse(actual, CultureInfo.InvariantCulture) == details[name].Value.GetInt64() / 100m, $"{expected.CaseId} {name} money mismatch.");
                }
            }
        }
    }

    private static void ValidateValidationSheet(ZipArchive archive, WorkbookCase expected)
    {
        var rows = ReadRows(archive.GetEntry("xl/worksheets/sheet2.xml")!);
        Require(rows[0][0] == InternalMark, "Internal-test marking is missing from Validation sheet.");
        Require(rows[1][0] == "case_id" && rows[1][1] == expected.CaseId, "Workbook case assignment mismatch.");
        Require(rows[2][0] == "scenario_id" && rows[2][1] == expected.Scenario, "Workbook scenario assignment mismatch.");
        Require(rows[3][0] == "workbook_id" && rows[3][1] == expected.WorkbookId, "Workbook identity mismatch.");
        Require(rows[4][1] == expected.AuthorizedContentSha256, "Authorized semantic commitment mismatch.");
        Require(rows[5][1] == expected.CasePackageSha256, "Case-package commitment mismatch.");
        Require(rows[6][1] == NormalizedSemanticHash(expected), "Normalized semantic content hash mismatch.");
        var headings = rows[7];
        var dataRows = rows.Skip(8).ToArray();
        Require(dataRows.Length == expected.AnnexRows.Count, "Validation-sheet Annex-row count mismatch.");
        for (var index = 0; index < dataRows.Length; index++)
        {
            var expectedRow = expected.AnnexRows.OrderBy(x => x.Ordinal).ElementAt(index);
            var actual = headings.Zip(dataRows[index]).ToDictionary(x => x.First, x => x.Second, StringComparer.Ordinal);
            Require(actual["annex_row_id"] == Value(expectedRow, "id").GetString(), $"{expectedRow.Key} row identity mismatch.");
            Require(actual["row_sequence"] == Value(expectedRow, "row_sequence").GetInt64().ToString(CultureInfo.InvariantCulture), $"{expectedRow.Key} sequence mismatch.");
            foreach (var member in ObjectMembers(expectedRow, "headers")) Require(actual[member.Key] == member.Value.Value.GetString(), $"{expectedRow.Key} {member.Key} mismatch.");
            foreach (var member in ObjectMembers(expectedRow, "details")) Require(actual[member.Key] == JsonScalar(member.Value.Value), $"{expectedRow.Key} {member.Key} mismatch.");
            foreach (var reconciliation in Reconciliations(expectedRow))
            {
                Require(actual[$"{reconciliation.Rule}_expected_minor_units"] == reconciliation.ExpectedMinorUnits.ToString(CultureInfo.InvariantCulture), $"{expectedRow.Key} {reconciliation.Rule} expected amount mismatch.");
                Require(actual[$"{reconciliation.Rule}_calculated_minor_units"] == reconciliation.CalculatedMinorUnits.ToString(CultureInfo.InvariantCulture), $"{expectedRow.Key} {reconciliation.Rule} calculated amount mismatch.");
                Require(actual[$"{reconciliation.Rule}_difference_minor_units"] == reconciliation.DifferenceMinorUnits.ToString(CultureInfo.InvariantCulture), $"{expectedRow.Key} {reconciliation.Rule} difference mismatch.");
                Require(actual[$"{reconciliation.Rule}_expected_row_count"] == reconciliation.ExpectedRowCount.ToString(CultureInfo.InvariantCulture), $"{expectedRow.Key} {reconciliation.Rule} expected count mismatch.");
                Require(actual[$"{reconciliation.Rule}_calculated_row_count"] == reconciliation.CalculatedRowCount.ToString(CultureInfo.InvariantCulture), $"{expectedRow.Key} {reconciliation.Rule} calculated count mismatch.");
                Require(actual[$"{reconciliation.Rule}_row_count_difference"] == reconciliation.RowCountDifference.ToString(CultureInfo.InvariantCulture), $"{expectedRow.Key} {reconciliation.Rule} count difference mismatch.");
                Require(actual[$"{reconciliation.Rule}_result"] == reconciliation.Result, $"{expectedRow.Key} {reconciliation.Rule} result mismatch.");
            }
        }
    }

    private static void ValidateFactLinkSheet(ZipArchive archive, WorkbookCase expected)
    {
        var rows = ReadRows(archive.GetEntry("xl/worksheets/sheet3.xml")!);
        Require(rows[0][0] == InternalMark, "Internal-test marking is missing from Fact Links sheet.");
        var headings = rows[1];
        var dataRows = rows.Skip(2).ToArray();
        Require(dataRows.Length == expected.FactLinks.Count, $"{expected.CaseId} fact-link count mismatch.");
        for (var index = 0; index < dataRows.Length; index++)
        {
            var link = expected.FactLinks.OrderBy(x => x.Ordinal).ElementAt(index);
            var actual = headings.Zip(dataRows[index]).ToDictionary(x => x.First, x => x.Second, StringComparer.Ordinal);
            foreach (var name in new[] { "id", "workbook_id", "period_id", "accounting_fact_id", "fact_type", "fact_semantic_hash", "source_ordinal", "created_at" })
                Require(actual[name] == JsonScalar(Value(link, name)), $"{link.Key} {name} mismatch.");
        }
    }

    private static void ValidateCellTypes(ZipArchive archive, WorkbookCase expected)
    {
        var main = ReadCellTypes(archive.GetEntry("xl/worksheets/sheet1.xml")!);
        for (var rowIndex = 0; rowIndex < expected.AnnexRows.Count; rowIndex++)
        {
            for (var index = 1; index <= 32; index++)
            {
                var reference = Column(index) + (17 + rowIndex).ToString(CultureInfo.InvariantCulture);
                Require(main[reference] == (index is <= 3 or 32 ? "inlineStr" : "number"), $"{expected.CaseId} {reference} cell type mismatch.");
            }
        }

        var validation = ReadCellTypes(archive.GetEntry("xl/worksheets/sheet2.xml")!);
        for (var rowIndex = 0; rowIndex < expected.AnnexRows.Count; rowIndex++)
        {
            var excelRow = 9 + rowIndex;
            for (var columnIndex = 1; columnIndex <= 131; columnIndex++)
            {
                var expectedType = columnIndex switch
                {
                    2 => "number",
                    >= 16 and <= 47 => columnIndex is <= 18 or 47 ? "inlineStr" : "number",
                    >= 48 => (columnIndex - 48) % 7 == 6 ? "inlineStr" : "number",
                    _ => "inlineStr"
                };
                Require(validation[Column(columnIndex) + excelRow.ToString(CultureInfo.InvariantCulture)] == expectedType, $"{expected.CaseId} Validation cell type mismatch at row {excelRow}, column {columnIndex}.");
            }
        }

        var factLinks = ReadCellTypes(archive.GetEntry("xl/worksheets/sheet3.xml")!);
        for (var rowIndex = 0; rowIndex < expected.FactLinks.Count; rowIndex++)
        {
            var excelRow = 3 + rowIndex;
            for (var columnIndex = 1; columnIndex <= 8; columnIndex++)
                Require(factLinks[Column(columnIndex) + excelRow.ToString(CultureInfo.InvariantCulture)] == (columnIndex == 7 ? "number" : "inlineStr"), $"{expected.CaseId} Fact Links cell type mismatch at row {excelRow}, column {columnIndex}.");
        }
    }

    private static byte[] BuildManifest(IReadOnlyList<ManifestEntry> entries, string aggregate)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("schemaVersion", "annex-e1-internal-workbook-manifest:v1.7");
            writer.WriteString("posture", InternalMark);
            writer.WriteString("datasetSha256", DatasetSha256);
            writer.WriteString("packageRootSha256", PackageRootSha256);
            writer.WriteNumber("workbookCount", entries.Count);
            writer.WriteNumber("annexRowCount", entries.Sum(x => x.AnnexRowCount));
            writer.WriteString("aggregateManifestSha256", aggregate);
            writer.WriteStartArray("workbooks");
            foreach (var entry in entries.OrderBy(x => x.CaseId, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("caseId", entry.CaseId);
                writer.WriteString("scenarioId", entry.Scenario);
                writer.WriteString("workbookId", entry.WorkbookId);
                writer.WriteString("fileName", entry.FileName);
                writer.WriteNumber("annexRowCount", entry.AnnexRowCount);
                writer.WriteNumber("byteLength", entry.ByteLength);
                writer.WriteString("workbookArtifactSha256", entry.WorkbookSha256);
                writer.WriteString("normalizedSemanticContentSha256", entry.NormalizedSemanticContentSha256);
                writer.WriteString("authorizedSemanticCommitmentSha256", entry.AuthorizedSemanticCommitmentSha256);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray().Concat([(byte)'\n']).ToArray();
    }

    private static string AggregateManifestHash(IReadOnlyList<ManifestEntry> entries)
    {
        var builder = new StringBuilder("ANNEX_E1_INTERNAL_WORKBOOK_MANIFEST=v1\n");
        foreach (var entry in entries.OrderBy(x => x.CaseId, StringComparer.Ordinal))
        {
            builder.Append(entry.CaseId).Append('|').Append(entry.Scenario).Append('|').Append(entry.WorkbookId).Append('|').Append(entry.FileName).Append('|')
                .Append(entry.AnnexRowCount).Append('|').Append(entry.ByteLength).Append('|').Append(entry.WorkbookSha256).Append('|')
                .Append(entry.NormalizedSemanticContentSha256).Append('|').Append(entry.AuthorizedSemanticCommitmentSha256).Append('\n');
        }
        return Sha256(Utf8NoBom.GetBytes(builder.ToString()));
    }

    private static IReadOnlyDictionary<string, Member> ObjectMembers(SemanticRow row, string name)
    {
        var member = row.Members.Single(x => x.Name == name);
        Require(member.Type == "object", $"{row.Key}.{name} is not an object.");
        return member.Value.EnumerateArray().Select(x => new Member(x.GetProperty("n").GetString()!, x.GetProperty("t").GetString()!, x.GetProperty("v").Clone()))
            .ToDictionary(x => x.Name, StringComparer.Ordinal);
    }

    private sealed record ReconciliationValue(string Rule, long ExpectedMinorUnits, long CalculatedMinorUnits, long DifferenceMinorUnits, long ExpectedRowCount, long CalculatedRowCount, long RowCountDifference, string Result);

    private static IReadOnlyList<ReconciliationValue> Reconciliations(SemanticRow row)
    {
        var member = row.Members.Single(x => x.Name == "reconciliations");
        Require(member.Type == "array", $"{row.Key}.reconciliations is not an array.");
        var values = member.Value.EnumerateArray().Select(x =>
        {
            var fields = x.GetProperty("v").EnumerateArray().ToDictionary(y => y.GetProperty("n").GetString()!, y => y.GetProperty("v").Clone(), StringComparer.Ordinal);
            return new ReconciliationValue(fields["rule"].GetString()!, fields["expected_minor_units"].GetInt64(), fields["calculated_minor_units"].GetInt64(), fields["difference_minor_units"].GetInt64(), fields["expected_row_count"].GetInt64(), fields["calculated_row_count"].GetInt64(), fields["row_count_difference"].GetInt64(), fields["result"].GetString()!);
        }).ToArray();
        Require(values.Select(x => x.Rule).SequenceEqual(Enumerable.Range(1, 12).Select(x => $"R{x:00}"), StringComparer.Ordinal), $"{row.Key} reconciliation ordering is incomplete or unstable.");
        return values;
    }

    private static JsonElement Value(SemanticRow row, string name) => row.Members.Single(x => x.Name == name).Value;

    private static string JsonScalar(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString()!,
        JsonValueKind.Number => value.GetInt64().ToString(CultureInfo.InvariantCulture),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => string.Empty,
        _ => throw new InvalidDataException($"Unsupported scalar kind {value.ValueKind}.")
    };

    private static Dictionary<string, string> ReadCells(ZipArchiveEntry entry)
    {
        var document = XDocument.Parse(ReadEntry(entry));
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return document.Descendants(ns + "c").ToDictionary(
            x => (string)x.Attribute("r")!,
            x => (string?)x.Attribute("t") == "inlineStr" ? string.Concat(x.Descendants(ns + "t").Select(t => t.Value)) : x.Element(ns + "v")?.Value ?? string.Empty,
            StringComparer.Ordinal);
    }

    private static Dictionary<string, string> ReadCellTypes(ZipArchiveEntry entry)
    {
        var document = XDocument.Parse(ReadEntry(entry));
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return document.Descendants(ns + "c").ToDictionary(
            x => (string)x.Attribute("r")!,
            x => (string?)x.Attribute("t") == "inlineStr" ? "inlineStr" : "number",
            StringComparer.Ordinal);
    }

    private static IReadOnlyList<IReadOnlyList<string>> ReadRows(ZipArchiveEntry entry)
    {
        var document = XDocument.Parse(ReadEntry(entry));
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return document.Descendants(ns + "row").Select(row => (IReadOnlyList<string>)row.Elements(ns + "c").Select(cell =>
            (string?)cell.Attribute("t") == "inlineStr" ? string.Concat(cell.Descendants(ns + "t").Select(t => t.Value)) : cell.Element(ns + "v")?.Value ?? string.Empty).ToArray()).ToArray();
    }

    private static void EqualCell(IReadOnlyDictionary<string, string> cells, string reference, string expected) => Require(cells.TryGetValue(reference, out var actual) && actual == expected, $"Cell {reference} mismatch.");
    private static string ReadEntry(ZipArchiveEntry entry) => Utf8NoBom.GetString(ReadEntryBytes(entry));
    private static byte[] ReadEntryBytes(ZipArchiveEntry entry) { using var stream = entry.Open(); using var memory = new MemoryStream(); stream.CopyTo(memory); return memory.ToArray(); }

    private static void AddEntry(ZipArchive archive, string name, byte[] content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
        entry.LastWriteTime = FixedZipTime;
        using var stream = entry.Open();
        stream.Write(content);
    }

    private static byte[] ReplaceUtf8(byte[] source, string oldValue, string newValue)
    {
        var text = Utf8NoBom.GetString(source);
        Require(text.Contains(oldValue, StringComparison.Ordinal), $"Open XML replacement anchor is missing: {oldValue}");
        return Utf8NoBom.GetBytes(text.Replace(oldValue, newValue, StringComparison.Ordinal));
    }

    private static string Column(int index) { var result = ""; while (index > 0) { index--; result = (char)('A' + index % 26) + result; index /= 26; } return result; }
    private static string Escape(string value) => System.Security.SecurityElement.Escape(value) ?? string.Empty;
    private static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }

    private sealed record CellValue(string? TextValue, long? IntegerValue)
    {
        internal static CellValue Text(string value) => new(value, null);
        internal static CellValue Integer(long value) => new(null, value);
    }
}
