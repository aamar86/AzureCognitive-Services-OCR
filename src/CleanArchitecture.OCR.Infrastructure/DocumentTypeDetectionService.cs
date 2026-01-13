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

        // IMPORTANT: Check for UAE Trade License FIRST
        // Trade License documents may contain passport information (for managers/owners),
        // so we need to check for Trade License before Passport to avoid false positives
        if (IsUAETradeLicense(normalizedText))
        {
            return DocumentType.UAETradeLicense;
        }

        // Check for Emirates ID - Look for Emirates ID number pattern (784-XXXX-XXXXXXX-X)
        if (IsEmiratesId(normalizedText))
        {
            return DocumentType.EmiratesID;
        }

        // Check for Passport - Look for MRZ lines starting with "P<"
        if (IsPassport(normalizedText))
        {
            return DocumentType.Passport;
        }

        // If no specific document type detected, return Passport as default
        // This allows backward compatibility
        return DocumentType.Passport;
    }

    private bool IsPassport(string text)
    {
        // Passport MRZ lines typically start with "P<" and contain many "<" characters
        // This is the strongest indicator of a passport document
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length >= 30)
            .ToList();

        // Check for MRZ line 1 starting with "P<" - this is definitive for passports
        var hasPassportMrz = lines.Any(l => l.StartsWith("P<") && l.Contains("<"));
        
        if (hasPassportMrz)
        {
            return true; // MRZ is definitive, return immediately
        }

        // Additional check: Look for passport-specific keywords
        // But exclude if Trade License indicators are present (to avoid false positives)
        var hasPassportKeywords = Regex.IsMatch(text, @"\b(PASSPORT|PASSPORT\s*NO|PASSPORT\s*NUMBER)\b", RegexOptions.IgnoreCase);
        
        if (hasPassportKeywords)
        {
            // Double-check: if we have passport keywords but also Trade License indicators,
            // it's likely a Trade License document that mentions passport (for manager/owner info)
            var hasTradeLicenseIndicators = Regex.IsMatch(text, @"\b(TRADE\s*LICENSE|LICENSE\s*NO|LICENSE\s*NUMBER|رخصة\s*تجارية|DEPARTMENT\s*OF\s*ECONOMIC\s*DEVELOPMENT|دائرة\s*التنمية\s*الاقتصادية)\b", RegexOptions.IgnoreCase);
            
            // Only return true if we have passport keywords AND no Trade License indicators
            return !hasTradeLicenseIndicators;
        }

        return false;
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
        // Score-based detection: count indicators that are specific to Trade License documents
        int score = 0;

        // 1. Check for Trade License title/keywords (high priority)
        var tradeLicenseTitlePatterns = new[]
        {
            @"\b(TRADE\s*LICENSE|TRADE\s*LICENCE)\b",  // English
            @"رخصة\s*تجارية",  // Arabic: رخصة تجارية
            @"BUSINESS\s*LICENSE|COMMERCIAL\s*LICENSE"
        };

        foreach (var pattern in tradeLicenseTitlePatterns)
        {
            if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
            {
                score += 3; // High weight for title
                break;
            }
        }

        // 2. Check for specific Trade License document fields (high priority)
        var tradeLicenseFieldPatterns = new[]
        {
            @"(?:LICENSE\s*NO|LICENSE\s*NUMBER|LICENSE\s*#)[\s:]*\d+",  // License No.: 123822
            @"(?:ACCI\s*NO|ACCI\s*NUMBER|رقم\s*الغرفة)[\s:]*\d+",  // ACCI No. or Arabic
            @"(?:REGISTER\s*NO|REGISTER\s*NUMBER|السجل\s*التجاري)[\s:]*\d+",  // Register No. or Arabic
            @"(?:TRADE\s*NAME|COMPANY\s*NAME|TRADING\s*NAME)",  // Trade Name
            @"(?:LEGAL\s*FORM|TYPE\s*OF\s*COMPANY)",  // Legal Form
            @"(?:EXPIRE\s*DATE|EXPIRY\s*DATE|تاريخ\s*الانتهاء)",  // Expire Date or Arabic
            @"(?:ISSUE\s*DATE|تاريخ\s*الاصدار)"  // Issue Date or Arabic
        };

        foreach (var pattern in tradeLicenseFieldPatterns)
        {
            if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
            {
                score += 2; // Medium-high weight for specific fields
            }
        }

        // 3. Check for UAE government departments (high priority)
        var governmentDepartmentPatterns = new[]
        {
            @"DEPARTMENT\s*OF\s*ECONOMIC\s*DEVELOPMENT",  // English
            @"دائرة\s*التنمية\s*الاقتصادية",  // Arabic: دائرة التنمية الاقتصادية
            @"GOVERNMENT\s*OF\s*AJMAN|GOVERNMENT\s*OF\s*DUBAI|GOVERNMENT\s*OF\s*ABU\s*DHABI",  // UAE Emirates
            @"حكومة\s*عجمان|حكومة\s*دبي|حكومة\s*أبو\s*ظبي"  // Arabic
        };

        foreach (var pattern in governmentDepartmentPatterns)
        {
            if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
            {
                score += 3; // High weight for government departments
                break;
            }
        }

        // 4. Check for Trade License number patterns (medium priority)
        // Pattern: License No.: followed by numbers (e.g., "123822")
        var licenseNumberWithLabel = Regex.IsMatch(text, @"(?:LICENSE\s*NO|LICENSE\s*NUMBER|LICENSE\s*#)[\s:]*(\d{5,})", RegexOptions.IgnoreCase);
        if (licenseNumberWithLabel)
        {
            score += 2;
        }

        // 5. Check for company/business related terms (low priority, but adds confidence)
        var businessTerms = new[]
        {
            @"LIMITED\s*LIABILITY\s*COMPANY|LLC|L\.L\.C",
            @"شركة\s*ذات\s*مسئولية\s*محدودة",  // Arabic: شركة ذات مسئولية محدودة
            @"ACTIVITIES|ACTIVITY|نشاط",  // Business activities
            @"LESSOR|مؤجر",  // Lessor (landlord)
            @"CONTRACT\s*EXPIRY|عقد\s*الايجار"  // Contract expiry
        };

        int businessTermCount = 0;
        foreach (var pattern in businessTerms)
        {
            if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
            {
                businessTermCount++;
            }
        }
        if (businessTermCount >= 2)
        {
            score += 1; // Add score if multiple business terms found
        }

        // Trade License detected if score is 4 or higher
        // This ensures we have strong indicators before classifying as Trade License
        return score >= 4;
    }
}

