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

                case DocumentType.UAETradeLicense:
                    result.UAETradeLicense = ParseUAETradeLicense(rawText);
                    result.IsValid = result.UAETradeLicense != null;
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

    // ===================== UAE TRADE LICENSE =====================

    private UAETradeLicenseResult ParseUAETradeLicense(string text)
    {
        var result = new UAETradeLicenseResult();
        var normalizedText = text.Replace("\r\n", "\n").Replace("\r", "\n");

        // Extract Trade License Number (common formats: numbers, alphanumeric)
        // Pattern: License number, Trade License, License No, etc.
        var licenseNumberPatterns = new[]
        {
            @"(?:Trade\s*License|License\s*Number|License\s*No|License\s*#)[\s:]*([A-Z0-9\-/]+)",
            @"(?:License|Lic\.)[\s:]*([A-Z0-9\-/]{6,})",
            @"\b([A-Z]{1,3}[-/]?\d{4,}[-/]?\d{2,})\b"
        };

        foreach (var pattern in licenseNumberPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                result.TradeLicenseNumber = match.Groups[1].Value.Trim();
                break;
            }
        }

        // Extract Company Name
        // Look for patterns like "Company Name:", "Name:", "Trading Name:", etc.
        var companyNamePatterns = new[]
        {
            @"(?:Company\s*Name|Trading\s*Name|Business\s*Name|Name\s*of\s*Company)[\s:]+([A-Za-z0-9\s&.,'-]+)",
            @"(?:Name)[\s:]+([A-Z][A-Za-z0-9\s&.,'-]{3,})"
        };

        foreach (var pattern in companyNamePatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var companyName = match.Groups[1].Value.Trim();
                // Filter out common false positives
                if (!companyName.Contains("License") && !companyName.Contains("Number") && 
                    companyName.Length > 3 && companyName.Length < 200)
                {
                    result.CompanyName = companyName;
                    break;
                }
            }
        }

        // Extract Expiry Date
        var expiryDatePatterns = new[]
        {
            @"(?:Expiry\s*Date|Expires|Valid\s*Until|Expiration)[\s:]+(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})",
            @"(?:Expiry|Expires)[\s:]+(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})",
            @"(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})" // Generic date pattern (last resort)
        };

        foreach (var pattern in expiryDatePatterns)
        {
            var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                var dateStr = match.Groups[1].Value;
                var parsedDate = ParseDate(dateStr);
                if (parsedDate.HasValue && parsedDate.Value > DateTime.Now)
                {
                    result.ExpiryDate = parsedDate;
                    break;
                }
            }
            if (result.ExpiryDate.HasValue) break;
        }

        // Extract Issue Date
        var issueDatePatterns = new[]
        {
            @"(?:Issue\s*Date|Issued\s*On|Date\s*of\s*Issue)[\s:]+(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})"
        };

        foreach (var pattern in issueDatePatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                result.IssueDate = ParseDate(match.Groups[1].Value);
                break;
            }
        }

        // Extract License Type
        var licenseTypePatterns = new[]
        {
            @"(?:License\s*Type|Type\s*of\s*License)[\s:]+([A-Za-z\s]+)",
            @"(?:Type)[\s:]+(Commercial|Professional|Industrial|General|Service)"
        };

        foreach (var pattern in licenseTypePatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                result.LicenseType = match.Groups[1].Value.Trim();
                break;
            }
        }

        // Extract Activity/Business Activity
        var activityPatterns = new[]
        {
            @"(?:Activity|Business\s*Activity|Main\s*Activity)[\s:]+([A-Za-z0-9\s&.,'-]+)",
            @"(?:Activity\s*Code|Activity\s*Description)[\s:]+([A-Za-z0-9\s&.,'-]+)"
        };

        foreach (var pattern in activityPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var activity = match.Groups[1].Value.Trim();
                if (activity.Length > 5 && activity.Length < 200)
                {
                    result.Activity = activity;
                    break;
                }
            }
        }

        // Extract Legal Form
        var legalFormPatterns = new[]
        {
            @"(?:Legal\s*Form|Form\s*of\s*Business)[\s:]+([A-Za-z\s]+)",
            @"(?:Form)[\s:]+(LLC|FZE|FZCO|Sole\s*Proprietorship|Partnership)"
        };

        foreach (var pattern in legalFormPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                result.LegalForm = match.Groups[1].Value.Trim();
                break;
            }
        }

        // Extract Address
        var addressPatterns = new[]
        {
            @"(?:Address|Registered\s*Address)[\s:]+([A-Za-z0-9\s,.-]+(?:P\.O\.\s*Box|PO\s*Box)?[A-Za-z0-9\s,.-]*)",
            @"(?:Location|Business\s*Address)[\s:]+([A-Za-z0-9\s,.-]+)"
        };

        foreach (var pattern in addressPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var address = match.Groups[1].Value.Trim();
                if (address.Length > 10 && address.Length < 300)
                {
                    result.Address = address;
                    break;
                }
            }
        }

        // Extract Emirate
        var emirates = new[] { "Dubai", "Abu Dhabi", "Sharjah", "Ajman", "Umm Al Quwain", "Ras Al Khaimah", "Fujairah" };
        foreach (var emirate in emirates)
        {
            if (Regex.IsMatch(text, $@"\b{emirate}\b", RegexOptions.IgnoreCase))
            {
                result.Emirate = emirate;
                break;
            }
        }

        // Extract Owner Name
        var ownerPatterns = new[]
        {
            @"(?:Owner|Proprietor|Manager|Partner)[\s:]+([A-Z][a-z]+(?:\s+[A-Z][a-z]+)+)",
            @"(?:Owner\s*Name|Name\s*of\s*Owner)[\s:]+([A-Za-z\s]+)"
        };

        foreach (var pattern in ownerPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var ownerName = match.Groups[1].Value.Trim();
                if (ownerName.Length > 3 && ownerName.Length < 100)
                {
                    result.OwnerName = ownerName;
                    break;
                }
            }
        }

        // Extract Owner Nationality
        var nationalityPatterns = new[]
        {
            @"(?:Owner\s*Nationality|Nationality)[\s:]+([A-Za-z\s]+)",
            @"(?:Nationality\s*of\s*Owner)[\s:]+([A-Za-z\s]+)"
        };

        foreach (var pattern in nationalityPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                result.OwnerNationality = match.Groups[1].Value.Trim();
                break;
            }
        }

        // Validate that at least essential fields are present
        if (string.IsNullOrWhiteSpace(result.TradeLicenseNumber) && 
            string.IsNullOrWhiteSpace(result.CompanyName))
        {
            throw new InvalidOperationException("UAE Trade License number or company name not detected");
        }

        return result;
    }

    private DateTime? ParseDate(string dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;

        // Try common date formats
        var formats = new[]
        {
            "dd/MM/yyyy", "dd-MM-yyyy", "dd.MM.yyyy",
            "dd/MM/yy", "dd-MM-yy", "dd.MM.yy",
            "MM/dd/yyyy", "MM-dd-yyyy", "MM.dd.yyyy",
            "yyyy/MM/dd", "yyyy-MM-dd", "yyyy.MM.dd"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture, 
                DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        // Fallback to standard DateTime parsing
        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, 
            DateTimeStyles.None, out var parsedDate))
        {
            return parsedDate;
        }

        return null;
    }
}
