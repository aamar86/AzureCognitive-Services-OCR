namespace CleanArchitecture.OCR.Application;

public interface IDocumentTypeDetectionService
{
    DocumentType DetectDocumentType(string rawText);
}


