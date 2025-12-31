using CleanArchitecture.OCR.Application;
using CleanArchitecture.OCR.Application.Exceptions;
using FastEndpoints;

namespace CleanArchitecture.OCR.API.Endpoints;

public class ProcessOCREndpoint : Endpoint<ProcessImageRequest, ProcessOCRResponse>
{
    private readonly IApplicationService _applicationService;
    private readonly IWebHostEnvironment _environment;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif", ".pdf" };

    public ProcessOCREndpoint(IApplicationService applicationService, IWebHostEnvironment environment)
    {
        _applicationService = applicationService;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("api/ocr/process");
        AllowAnonymous();
        AllowFileUploads();
        Description(b => b
            .Produces<ProcessOCRResponse>(200)
            .ProducesProblem(400)
            .WithTags("OCR"));
    }

    public override async Task HandleAsync(ProcessImageRequest req, CancellationToken ct)
    {
        string? filePath = null;
        try
        {
            // If file is uploaded, save it temporarily
            if (Files.Count > 0)
            {
                var file = Files[0];
                
                // Validate file extension for uploaded files
                var fileExtension = Path.GetExtension(file.Name)?.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(fileExtension) || !AllowedExtensions.Contains(fileExtension))
                {
                    ThrowError($"Invalid file format. Allowed formats are: {string.Join(", ", AllowedExtensions)}. Provided: {fileExtension ?? "none"}");
                    return;
                }

                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                Directory.CreateDirectory(uploadsPath);
                
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                filePath = Path.Combine(uploadsPath, fileName);
                
                await using var fileStream = new FileStream(filePath, FileMode.Create);
                await file.OpenReadStream().CopyToAsync(fileStream, ct);
            }
            else if (!string.IsNullOrWhiteSpace(req.ImagePath))
            {
                // Use provided file path
                filePath = req.ImagePath;
            }
            else
            {
                ThrowError("Either a file must be uploaded or ImagePath must be provided.");
                return;
            }

            // Process OCR
            OcrResult result;
            if (req.DocumentType.HasValue)
            {
                result = await _applicationService.ProcessOCRAsync(filePath, req.DocumentType.Value);
            }
            else
            {
                result = await _applicationService.ProcessOCRAsync(filePath);
            }
            
            // Clean up uploaded file if it was uploaded
            if (Files.Count > 0 && filePath != null && File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            
            await SendOkAsync(new ProcessOCRResponse 
            { 
                IsValid = result.IsValid,
                DocumentType = result.DocumentType,
                RawText = result.RawText,
                Passport = result.Passport,
                EmiratesId = result.EmiratesId,
                UAETradeLicense = result.UAETradeLicense,
                Errors = result.Errors
            }, ct);
        }
        catch (InvalidDocumentTypeException ex)
        {
            Logger.LogWarning(ex, "Document type mismatch detected");
            
            // Clean up uploaded file if it was uploaded
            if (Files.Count > 0 && filePath != null && File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            
            ThrowError(ex.Message);
        }
        catch (FileNotFoundException ex)
        {
            Logger.LogWarning(ex, "File not found");
            ThrowError(ex.Message);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Invalid argument");
            ThrowError(ex.Message);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing OCR");
            
            // Clean up uploaded file if it was uploaded
            if (Files.Count > 0 && filePath != null && File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            
            ThrowError(ex.Message);
        }
    }
}

public class ProcessImageRequest
{
    public string? ImagePath { get; set; }
    public DocumentType? DocumentType { get; set; }
}

public class ProcessOCRResponse
{
    public bool IsValid { get; set; }
    public DocumentType DocumentType { get; set; }
    public string RawText { get; set; } = string.Empty;
    public PassportResult? Passport { get; set; }
    public EmiratesIdResult? EmiratesId { get; set; }
    public UAETradeLicenseResult? UAETradeLicense { get; set; }
    public List<string> Errors { get; set; } = new();
}

