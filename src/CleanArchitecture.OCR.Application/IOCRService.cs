namespace CleanArchitecture.OCR.Application;

public interface IOCRService
{
    Task<string> ExtractTextAsync(string imagePath);
}

