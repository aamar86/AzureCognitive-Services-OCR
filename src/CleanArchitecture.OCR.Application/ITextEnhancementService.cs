namespace CleanArchitecture.OCR.Application;

/// <summary>
/// Service for enhancing and structuring OCR-extracted text using LLM capabilities
/// </summary>
public interface ITextEnhancementService
{
    /// <summary>
    /// Enhances OCR text by improving interpretation, correcting errors, and structuring the content
    /// </summary>
    /// <param name="rawOcrText">The raw text extracted from OCR</param>
    /// <param name="documentType">The type of document being processed</param>
    /// <returns>Enhanced and structured text</returns>
    Task<string> EnhanceTextAsync(string rawOcrText, DocumentType documentType, CancellationToken cancellationToken = default);
}

