using CleanArchitecture.OCR.Application;
using FastEndpoints;

namespace CleanArchitecture.OCR.API.Endpoints;

public class ProcessOCREndpoint : Endpoint<ProcessImageRequest, ProcessOCRResponse>
{
    private readonly IApplicationService _applicationService;
    private readonly IWebHostEnvironment _environment;

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
        try
        {
            string filePath;
            
            // If file is uploaded, save it temporarily
            if (Files.Count > 0)
            {
                var file = Files[0];
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                Directory.CreateDirectory(uploadsPath);
                
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
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
            string result;
            if (req.DocumentType.HasValue)
            {
                result = await _applicationService.ProcessOCRAsync(filePath, req.DocumentType.Value);
            }
            else
            {
                result = await _applicationService.ProcessOCRAsync(filePath);
            }
            
            // Clean up uploaded file if it was uploaded
            if (Files.Count > 0 && File.Exists(filePath))
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
            
            await SendOkAsync(new ProcessOCRResponse { Result = result }, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing OCR");
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
    public string Result { get; set; } = string.Empty;
}

