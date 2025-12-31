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

    public ApplicationService(IOCRService ocrService, IDocumentParsingService documentParsingService)
    {
        _ocrService = ocrService;
        _documentParsingService = documentParsingService;
    }

    public async Task<OcrResult> ProcessOCRAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        }

        var rawText = await _ocrService.ExtractTextAsync(filePath);
        return _documentParsingService.Parse(rawText, DocumentType.Passport);
    }

    public async Task<OcrResult> ProcessOCRAsync(string filePath, DocumentType documentType)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        }

        var rawText = await _ocrService.ExtractTextAsync(filePath, documentType);
        return _documentParsingService.Parse(rawText, documentType);
    }
}
