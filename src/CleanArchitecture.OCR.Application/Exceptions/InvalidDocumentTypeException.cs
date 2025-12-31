using CleanArchitecture.OCR.Application;

namespace CleanArchitecture.OCR.Application.Exceptions;

public class InvalidDocumentTypeException : Exception
{
    public DocumentType ExpectedType { get; }
    public DocumentType DetectedType { get; }

    public InvalidDocumentTypeException(DocumentType expectedType, DocumentType detectedType)
        : base($"Document type mismatch. Expected: {expectedType}, but detected: {detectedType}. The uploaded document does not match the selected document type.")
    {
        ExpectedType = expectedType;
        DetectedType = detectedType;
    }
}

