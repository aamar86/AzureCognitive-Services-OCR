using CleanArchitecture.OCR.Domain;

namespace CleanArchitecture.OCR.Application;

public interface IApplicationService
{
    Task<string> ProcessOCRAsync(string filePath);
    Task<string> ProcessOCRAsync(string filePath, DocumentType documentType);
}

public class ApplicationService : IApplicationService
{
    private readonly IOCRService _ocrService;

    public ApplicationService(IOCRService ocrService)
    {
        _ocrService = ocrService;
    }

    public async Task<string> ProcessOCRAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        }

        var result = await _ocrService.ExtractTextAsync(filePath);
        return result;
    }

    public async Task<string> ProcessOCRAsync(string filePath, DocumentType documentType)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        }

        var result = await _ocrService.ExtractTextAsync(filePath, documentType);
        return result;
    }
}
