using CleanArchitecture.OCR.Domain;

namespace CleanArchitecture.OCR.Application;

public interface IApplicationService
{
    Task<string> ProcessOCRAsync(string imagePath);
}

public class ApplicationService : IApplicationService
{
    private readonly IOCRService _ocrService;

    public ApplicationService(IOCRService ocrService)
    {
        _ocrService = ocrService;
    }

    public async Task<string> ProcessOCRAsync(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentException("Image path cannot be empty", nameof(imagePath));
        }

        var result = await _ocrService.ExtractTextAsync(imagePath);
        return result;
    }
}
