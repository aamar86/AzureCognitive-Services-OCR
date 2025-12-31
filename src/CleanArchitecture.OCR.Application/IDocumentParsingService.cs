using CleanArchitecture.OCR.Application;

public interface IDocumentParsingService
{
    OcrResult Parse(string rawText, DocumentType documentType);
}
