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

    public ApplicationService(
        IOCRService ocrService, 
        IDocumentParsingService documentParsingService,
        IDocumentTypeDetectionService documentTypeDetectionService)
    {
        _ocrService = ocrService;
        _documentParsingService = documentParsingService;
        _documentTypeDetectionService = documentTypeDetectionService;
    }

    public async Task<OcrResult> ProcessOCRAsync(string filePath)
    {
        ValidateFilePath(filePath);

        var rawText = await _ocrService.ExtractTextAsync(filePath);
        return _documentParsingService.Parse(rawText, DocumentType.Passport);
    }

    public async Task<OcrResult> ProcessOCRAsync(string filePath, DocumentType documentType)
    {
        ValidateFilePath(filePath);
        ValidateFileExtension(filePath);

        // Extract text first
        var rawText = await _ocrService.ExtractTextAsync(filePath, documentType);

        // Detect the actual document type from the extracted text
        var detectedType = _documentTypeDetectionService.DetectDocumentType(rawText);

        // Validate that the detected type matches the expected type
        if (detectedType != documentType)
        {
            throw new InvalidDocumentTypeException(documentType, detectedType);
        }

        // Parse the document with the expected type
        return _documentParsingService.Parse(rawText, documentType);
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
