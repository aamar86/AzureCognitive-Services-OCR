using CleanArchitecture.OCR.Application;

public class OcrResult
{
    public DocumentType DocumentType { get; set; }
    public bool IsValid { get; set; }
    public string RawText { get; set; } = string.Empty;
    public PassportResult? Passport { get; set; }
    public EmiratesIdResult? EmiratesId { get; set; }
    public List<string> Errors { get; set; } = new();
}
