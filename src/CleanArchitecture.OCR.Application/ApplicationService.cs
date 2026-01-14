using CleanArchitecture.OCR.Application.Exceptions;
using CleanArchitecture.OCR.Domain;

namespace CleanArchitecture.OCR.Application;

public interface IApplicationService
{
    Task<OcrResult> ProcessOCRAsync(string filePath);
    Task<OcrResult> ProcessOCRAsync(string filePath, DocumentType documentType);
}

public class ApplicationService : IApplicationService
{
    private readonly IOCRService _ocrService;
    private readonly IDocumentParsingService _documentParsingService;
    private readonly IDocumentTypeDetectionService _documentTypeDetectionService;
    private readonly ITextEnhancementService? _textEnhancementService;

    public ApplicationService(
        IOCRService ocrService, 
        IDocumentParsingService documentParsingService,
        IDocumentTypeDetectionService documentTypeDetectionService,
        ITextEnhancementService? textEnhancementService = null)
    {
        _ocrService = ocrService;
        _documentParsingService = documentParsingService;
        _documentTypeDetectionService = documentTypeDetectionService;
        _textEnhancementService = textEnhancementService;
    }

    public async Task<OcrResult> ProcessOCRAsync(string filePath)
    {
        ValidateFilePath(filePath);

        var rawText = await _ocrService.ExtractTextAsync(filePath);
        
        // Enhance text using Gemma LLM if available
        var enhancedText = _textEnhancementService != null
            ? await _textEnhancementService.EnhanceTextAsync(rawText, DocumentType.Passport)
            : rawText;
        
        return _documentParsingService.Parse(enhancedText, DocumentType.Passport);
    }

    public async Task<OcrResult> ProcessOCRAsync(string filePath, DocumentType documentType)
    {
        ValidateFilePath(filePath);
        ValidateFileExtension(filePath);

        // Extract text first
        var rawText = await _ocrService.ExtractTextAsync(filePath, documentType);

        // Enhance text using Gemma LLM if available
        var enhancedText = _textEnhancementService != null
            ? await _textEnhancementService.EnhanceTextAsync(rawText, documentType)
            : rawText;

        // Detect the actual document type from the enhanced text
        var detectedType = _documentTypeDetectionService.DetectDocumentType(enhancedText);

        // Validate that the detected type matches the expected type
        if (detectedType != documentType)
        {
            throw new InvalidDocumentTypeException(documentType, detectedType);
        }

        // Parse the document with the expected type
        return _documentParsingService.Parse(enhancedText, documentType);
    }

    private void ValidateFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }
    }

    private void ValidateFileExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif", ".pdf" };

        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
        {
            throw new ArgumentException(
                $"Invalid file format. Allowed formats are: {string.Join(", ", allowedExtensions)}. Provided: {extension ?? "none"}",
                nameof(filePath));
        }
    }
}
