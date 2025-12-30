using CleanArchitecture.OCR.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Drawing;
using System.Drawing.Imaging;
using Tesseract;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using PdfiumViewer;
using PdfiumDocument = PdfiumViewer.PdfDocument;

namespace CleanArchitecture.OCR.Infrastructure;

public class TesseractOCRService : IOCRService
{
    private readonly TesseractOCRSettings _settings;
    private readonly ILogger<TesseractOCRService>? _logger;

    public TesseractOCRService(IOptions<TesseractOCRSettings> settings, ILogger<TesseractOCRService>? logger = null)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(string filePath)
    {
        return await ExtractTextAsync(filePath, DocumentType.Passport);
    }

    public async Task<string> ExtractTextAsync(string filePath, DocumentType documentType)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            // Handle PDF files
            if (extension == ".pdf")
            {
                return await ProcessPdfAsync(filePath, documentType);
            }
            
            // Handle image files
            if (IsImageFile(extension))
            {
                return await ProcessImageAsync(filePath, documentType);
            }

            throw new NotSupportedException($"File format '{extension}' is not supported. Supported formats: PDF, PNG, JPG, JPEG, BMP, TIFF");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing OCR for file: {FilePath}", filePath);
            throw new InvalidOperationException($"Error processing OCR: {ex.Message}", ex);
        }
    }

    private async Task<string> ProcessPdfAsync(string pdfPath, DocumentType documentType)
    {
        var extractedText = new System.Text.StringBuilder();
        
        return await Task.Run(() =>
        {
            try
            {
                // First, try to extract text directly from PDF (if it's text-based PDF)
                using var document = UglyToad.PdfPig.PdfDocument.Open(pdfPath);
                var hasText = false;
                
                foreach (var page in document.GetPages())
                {
                    var pageText = page.Text;
                    
                    // If PDF contains extractable text, use it
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        hasText = true;
                        extractedText.AppendLine($"--- Page {page.Number} ---");
                        extractedText.AppendLine(pageText);
                        extractedText.AppendLine();
                    }
                }
                
                // If we found text, return it
                if (hasText)
                {
                    return extractedText.ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to extract text directly from PDF: {Error}", ex.Message);
            }

            // If no text found or extraction failed, render PDF pages to images and use OCR
            _logger?.LogInformation("PDF appears to be image-based, rendering pages for OCR");
            return ProcessImageBasedPdfAsync(pdfPath, documentType);
        });
    }

    private string ProcessImageBasedPdfAsync(string pdfPath, DocumentType documentType)
    {
        var extractedText = new System.Text.StringBuilder();
        
        try
        {
            using var document = PdfiumDocument.Load(pdfPath);
            var pageCount = document.PageCount;
            
            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                // Render PDF page to image at 300 DPI for better OCR quality
                const int dpi = 300;
                using var image = document.Render(pageIndex, dpi, dpi, PdfRenderFlags.Annotations);
                
                // Process image with Tesseract
                var pageText = ProcessImageWithTesseract(image, documentType);
                extractedText.AppendLine($"--- Page {pageIndex + 1} ---");
                extractedText.AppendLine(pageText);
                extractedText.AppendLine();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error rendering PDF pages to images");
            throw new InvalidOperationException(
                "Failed to process image-based PDF. Ensure PdfiumViewer native dependencies (pdfium.dll) are installed. " +
                "For Windows, download from: https://github.com/pvginkel/PdfiumViewer/releases. " +
                "Original error: " + ex.Message, ex);
        }

        return extractedText.ToString().Trim();
    }

    private async Task<string> ProcessImageAsync(string imagePath, DocumentType documentType)
    {
        return await Task.Run(() =>
        {
            using var image = Image.FromFile(imagePath);
            return ProcessImageWithTesseract(image, documentType);
        });
    }

    private string ProcessImageWithTesseract(Image image, DocumentType documentType)
    {
        var tessDataPath = _settings.TessDataPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
        
        if (!Directory.Exists(tessDataPath))
        {
            throw new DirectoryNotFoundException($"Tesseract data directory not found: {tessDataPath}. Please ensure Tesseract language data files are available.");
        }

        using var engine = new TesseractEngine(tessDataPath, _settings.Language ?? "eng", EngineMode.Default);
        
        // Configure Tesseract for document types
        ConfigureTesseractForDocumentType(engine, documentType);

        // Convert image to Pix format for Tesseract
        using var pix = ConvertImageToPix(image);
        using var page = engine.Process(pix);

        var text = page.GetText();
        return text.Trim();
    }

    private void ConfigureTesseractForDocumentType(TesseractEngine engine, DocumentType documentType)
    {
        // Set page segmentation mode for better results with documents
        engine.SetVariable("tessedit_pageseg_mode", "6"); // Assume uniform block of text
        
        // Configure for passport/ID documents
        if (documentType == DocumentType.Passport)
        {
            // Passport-specific configurations
            engine.SetVariable("tessedit_char_whitelist", 
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789<");
            engine.SetVariable("tessedit_pageseg_mode", "6");
        }
        else if (documentType == DocumentType.EmiratesID)
        {
            // Emirates ID specific configurations
            engine.SetVariable("tessedit_char_whitelist", 
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");
            engine.SetVariable("tessedit_pageseg_mode", "6");
        }
    }

    private Pix ConvertImageToPix(Image image)
    {
        // Convert System.Drawing.Image to Tesseract Pix
        using var ms = new MemoryStream();
        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        ms.Position = 0;
        
        return Pix.LoadFromMemory(ms.ToArray());
    }


    private bool IsImageFile(string extension)
    {
        var supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".tif", ".gif" };
        return supportedExtensions.Contains(extension);
    }
}

public class TesseractOCRSettings
{
    public const string SectionName = "TesseractOCR";
    
    public string? TessDataPath { get; set; }
    public string Language { get; set; } = "eng";
}

