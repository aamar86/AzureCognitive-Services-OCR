namespace CleanArchitecture.OCR.Application;

public enum DocumentType
{
    Passport,
    EmiratesID,
    UAETradeLicense
}

public interface IOCRService
{
    Task<string> ExtractTextAsync(string filePath);
    Task<string> ExtractTextAsync(string filePath, DocumentType documentType);
}

