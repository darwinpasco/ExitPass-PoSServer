using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using System.Xml.Linq;

namespace ExitPass.PosServer.Runtime.Tests.FiscalReports;

internal static class AnnexE1OpenXmlStandardsValidator
{
    internal static void Validate(string path)
    {
        try
        {
            using var document = SpreadsheetDocument.Open(path, false, new OpenSettings { AutoSave = false });
            var errors = new OpenXmlValidator(FileFormatVersions.Office2019).Validate(document).ToArray();
            if (errors.Length > 0)
            {
                var details = errors.Select(error => string.Join(
                    " | ",
                    $"part={error.Part?.Uri?.OriginalString ?? "<package>"}",
                    $"element={error.Node?.LocalName ?? "<none>"}",
                    $"rule={error.Id ?? "<none>"}",
                    $"path={error.Path?.XPath ?? "<none>"}",
                    $"description={error.Description}"));
                throw new InvalidDataException("Open XML validation failed:" + Environment.NewLine + string.Join(Environment.NewLine, details));
            }

            ValidateExcelInteroperabilityProfile(document);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is OpenXmlPackageException or FileFormatException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException($"Open XML package validation failed for '{Path.GetFileName(path)}': {exception.Message}", exception);
        }
    }

    private static void ValidateExcelInteroperabilityProfile(SpreadsheetDocument document)
    {
        var workbookPart = document.WorkbookPart ?? throw new InvalidDataException("Open XML package has no workbook part.");
        using var stream = workbookPart.GetStream(FileMode.Open, FileAccess.Read);
        var workbook = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var fileVersion = workbook.Root?.Element(ns + "fileVersion");
        if (fileVersion is null) return;

        var missing = new[] { "lastEdited", "lowestEdited", "rupBuild" }
            .Where(name => fileVersion.Attribute(name) is null)
            .ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidDataException(string.Join(
                " | ",
                "part=/xl/workbook.xml",
                "element=fileVersion",
                "rule=EXCEL_FILEVERSION_INTEROPERABILITY",
                "path=/workbook/fileVersion[1]",
                $"description=Incomplete fileVersion is rejected by Microsoft Excel; missing {string.Join(", ", missing)}."));
        }
    }
}
