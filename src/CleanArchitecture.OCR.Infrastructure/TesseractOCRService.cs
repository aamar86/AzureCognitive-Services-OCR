using CleanArchitecture.OCR.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Drawing;
using System.Drawing.Imaging;
using Tesseract;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using PdfiumViewer;

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
        return await Task.Run(() =>
        {
            try
            {
                // Extract text directly from PDF using PdfPig
                using var document = UglyToad.PdfPig.PdfDocument.Open(pdfPath);
                var extractedText = new System.Text.StringBuilder();
                var hasText = false;
                
                foreach (var page in document.GetPages())
                {
                    var pageText = page.Text;
                    
                    // Extract text from the page
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        hasText = true;
                        extractedText.AppendLine($"--- Page {page.Number} ---");
                        extractedText.AppendLine(pageText);
                        extractedText.AppendLine();
                    }
                    else
                    {
                        // Try to extract text from words if page.Text is empty
                        var words = page.GetWords();
                        if (words.Any())
                        {
                            hasText = true;
                            extractedText.AppendLine($"--- Page {page.Number} ---");
                            
                            // Group words by line (approximate based on Y position)
                            var wordsByLine = words
                                .OrderByDescending(w => w.BoundingBox.Bottom)
                                .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
                                .OrderByDescending(g => g.Key);
                            
                            foreach (var line in wordsByLine)
                            {
                                var lineText = string.Join(" ", line.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));
                                extractedText.AppendLine(lineText);
                            }
                            extractedText.AppendLine();
                        }
                    }
                }
                
                // If we found text, return it
                if (hasText)
                {
                    return extractedText.ToString().Trim();
                }
                
                // If no text found, the PDF is likely image-based (scanned)
                // Convert PDF pages to images and process with OCR
                _logger?.LogInformation("PDF appears to be image-based. Converting pages to images for OCR processing.");
                return ProcessImageBasedPdfAsync(pdfPath, documentType);
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                // If PdfPig fails, try converting PDF to images
                _logger?.LogWarning(ex, "Failed to extract text directly from PDF. Attempting to convert PDF to images: {Error}", ex.Message);
                try
                {
                    return ProcessImageBasedPdfAsync(pdfPath, documentType);
                }
                catch (Exception conversionEx)
                {
                    _logger?.LogError(conversionEx, "Failed to process PDF as image-based: {Error}", conversionEx.Message);
                    throw new InvalidOperationException(
                        $"Failed to extract text from PDF. The PDF may be corrupted or image-based. " +
                        $"Original error: {ex.Message}. Conversion error: {conversionEx.Message}", ex);
                }
            }
        });
    }

    private string ProcessImageBasedPdfAsync(string pdfPath, DocumentType documentType)
    {
        var extractedText = new System.Text.StringBuilder();

        try
        {
            // Open PDF with PdfiumViewer
            using var document = PdfiumViewer.PdfDocument.Load(pdfPath);
            var pageCount = document.PageCount;

            _logger?.LogInformation("Converting {PageCount} PDF pages to images for OCR", pageCount);

            // Process each page - render at 300 DPI for good OCR quality
            const int dpi = 300;
            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                // Render page to bitmap at specified DPI
                using var bitmap = document.Render(pageIndex, dpi, dpi, true);
                
                // Process the rendered image with OCR
                var pageText = ProcessImageWithTesseract(bitmap, documentType);
                
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    extractedText.AppendLine($"--- Page {pageIndex + 1} ---");
                    extractedText.AppendLine(pageText);
                    extractedText.AppendLine();
                }
            }

            var result = extractedText.ToString().Trim();
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException("No text could be extracted from the PDF pages using OCR.");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing image-based PDF: {Error}", ex.Message);
            throw new InvalidOperationException(
                $"Failed to process PDF as image-based document: {ex.Message}", ex);
        }
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

