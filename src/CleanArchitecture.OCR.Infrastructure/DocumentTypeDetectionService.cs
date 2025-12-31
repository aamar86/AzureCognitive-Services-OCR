using CleanArchitecture.OCR.Application;
using System.Text.RegularExpressions;

namespace CleanArchitecture.OCR.Infrastructure;

public class DocumentTypeDetectionService : IDocumentTypeDetectionService
{
    public DocumentType DetectDocumentType(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            throw new ArgumentException("Raw text cannot be empty", nameof(rawText));
        }

        var normalizedText = rawText.Replace("\r\n", "\n").Replace("\r", "\n");

        // Check for Passport - Look for MRZ lines starting with "P<"
        if (IsPassport(normalizedText))
        {
            return DocumentType.Passport;
        }

        // Check for Emirates ID - Look for Emirates ID number pattern (784-XXXX-XXXXXXX-X)
        if (IsEmiratesId(normalizedText))
        {
            return DocumentType.EmiratesID;
        }

        // Check for UAE Trade License - Look for trade license indicators
        if (IsUAETradeLicense(normalizedText))
        {
            return DocumentType.UAETradeLicense;
        }

        // If no specific document type detected, return Passport as default
        // This allows backward compatibility
        return DocumentType.Passport;
    }

    private bool IsPassport(string text)
    {
        // Passport MRZ lines typically start with "P<" and contain many "<" characters
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length >= 30)
            .ToList();

        // Check for MRZ line 1 starting with "P<"
        var hasPassportMrz = lines.Any(l => l.StartsWith("P<") && l.Contains("<"));
        
        // Additional check: Look for passport-specific keywords
        var hasPassportKeywords = Regex.IsMatch(text, @"\b(PASSPORT|PASSPORT\s*NO|PASSPORT\s*NUMBER)\b", RegexOptions.IgnoreCase);

        return hasPassportMrz || hasPassportKeywords;
    }

    private bool IsEmiratesId(string text)
    {
        // Emirates ID has a specific number pattern: 784-XXXX-XXXXXXX-X or 784XXXXXXXXXXXXX (15 digits starting with 784)
        // Pattern matches with or without hyphens
        var emiratesIdPatternWithHyphens = @"784-\d{4}-\d{7}-\d";
        var emiratesIdPatternWithoutHyphens = @"784\d{12}"; // 15 digits total: 784 (3) + 12 more digits
        
        if (Regex.IsMatch(text, emiratesIdPatternWithHyphens) || Regex.IsMatch(text, emiratesIdPatternWithoutHyphens))
        {
            return true;
        }

        // Additional check: Look for Emirates ID specific keywords
        var hasEmiratesIdKeywords = Regex.IsMatch(text, @"\b(EMIRATES\s*ID|EMIRATES\s*IDENTITY|UNITED\s*ARAB\s*EMIRATES\s*ID|RESIDENT\s*IDENTITY\s*CARD|FEDERAL\s*AUTHORITY\s*FOR\s*IDENTITY)\b", RegexOptions.IgnoreCase);
        
        return hasEmiratesIdKeywords;
    }

    private bool IsUAETradeLicense(string text)
    {
        // Look for trade license specific keywords
        var tradeLicenseKeywords = new[]
        {
            @"\b(TRADE\s*LICENSE|TRADE\s*LICENCE|BUSINESS\s*LICENSE|COMMERCIAL\s*LICENSE)\b",
            @"\b(TRADE\s*LICENSE\s*NUMBER|LICENSE\s*NUMBER|LICENSE\s*NO)\b",
            @"\b(UAE\s*TRADE\s*LICENSE|DUBAI\s*TRADE\s*LICENSE)\b"
        };

        foreach (var pattern in tradeLicenseKeywords)
        {
            if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        // Check for common trade license number patterns
        var licenseNumberPattern = @"\b([A-Z]{1,3}[-/]?\d{4,}[-/]?\d{2,})\b";
        if (Regex.IsMatch(text, licenseNumberPattern))
        {
            // Additional validation: Check if it's not a passport or Emirates ID
            if (!IsPassport(text) && !IsEmiratesId(text))
            {
                return true;
            }
        }

        return false;
    }
}

