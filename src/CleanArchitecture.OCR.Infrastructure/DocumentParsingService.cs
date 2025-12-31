using CleanArchitecture.OCR.Application;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CleanArchitecture.OCR.Infrastructure;

public class DocumentParsingService : IDocumentParsingService
{
    public OcrResult Parse(string rawText, DocumentType documentType)
    {
        var result = new OcrResult
        {
            RawText = rawText,
            DocumentType = documentType
        };

        try
        {
            switch (documentType)
            {
                case DocumentType.Passport:
                    result.Passport = ParsePassport(rawText);
                    result.IsValid = result.Passport != null;
                    break;

                case DocumentType.EmiratesID:
                    result.EmiratesId = ParseEmiratesId(rawText);
                    result.IsValid = result.EmiratesId != null;
                    break;

                default:
                    throw new NotSupportedException("Unsupported document type");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
            result.IsValid = false;
        }

        return result;
    }

    // ===================== PASSPORT =====================

    private PassportResult ParsePassport(string rawText)
    {
        var (mrzLine1, mrzLine2) = ExtractMrzLines(rawText);

       // mrzLine1 = NormalizeMrz(mrzLine1);
       // mrzLine2 = NormalizeMrz(mrzLine2);

        var names = mrzLine1.Substring(5).Split("<<", StringSplitOptions.RemoveEmptyEntries);

        return new PassportResult
        {
            MrzLine1 = mrzLine1,
            MrzLine2 = mrzLine2,
            CountryCode = mrzLine1.Substring(2, 3),
            Surname = names.Length > 0 ? names[0].Replace("<", " ").Trim() : "",
            GivenNames = names.Length > 1 ? names[1].Replace("<", " ").Trim() : "",
            PassportNumber = mrzLine2.Substring(0, 9).Replace("<", "").Trim(),
            Nationality = mrzLine2.Substring(10, 3),
            DateOfBirth = ParseMrzDate(mrzLine2.Substring(13, 6)),
            Sex = mrzLine2.Substring(20, 1),
            ExpiryDate = ParseMrzDate(mrzLine2.Substring(21, 6))
        };
    }

    // ===================== MRZ EXTRACTION =====================

    private (string line1, string line2) ExtractMrzLines(string rawText)
    {
        var candidates = rawText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Contains("<"))
            .Where(l => l.Length >= 30)
            .ToList();

        if (!candidates.Any())
            throw new InvalidOperationException("No MRZ candidates found");

        // Line 1: must start with P<
        var line1 = candidates
            .FirstOrDefault(l => l.StartsWith("P<"));

        if (line1 == null)
            throw new InvalidOperationException("MRZ line 1 not detected");

        // Line 2: highest '<' count excluding line1
        var line2 = candidates
            .Where(l => l != line1)
            .OrderByDescending(l => l.Count(c => c == '<'))
            .FirstOrDefault();

        if (line2 == null)
            throw new InvalidOperationException("MRZ line 2 not detected");

        return (line1, line2);
    }

    // ===================== MRZ NORMALIZATION =====================

    //private string NormalizeMrz(string mrz)
    //{
    //    return mrz
    //        .Replace(" ", "")
    //        .Replace("O", "0")
    //        .Replace("I", "1")
    //        .Replace("B", "8");
    //}

    // ===================== DATE =====================

    private DateTime? ParseMrzDate(string value)
    {
        if (!DateTime.TryParseExact(value, "yyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date))
            return null;

        return date > DateTime.UtcNow ? date: date;
    }

    // ===================== EMIRATES ID =====================

    private EmiratesIdResult ParseEmiratesId(string text)
    {
        var idMatch = Regex.Match(text, @"784-\d{4}-\d{7}-\d");
        if (!idMatch.Success)
            throw new InvalidOperationException("Emirates ID number not detected");

        return new EmiratesIdResult
        {
            IdNumber = idMatch.Value
        };
    }
}
