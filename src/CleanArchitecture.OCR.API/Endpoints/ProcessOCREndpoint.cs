using CleanArchitecture.OCR.Application;
using FastEndpoints;

namespace CleanArchitecture.OCR.API.Endpoints;

public class ProcessOCREndpoint : Endpoint<ProcessImageRequest, ProcessOCRResponse>
{
    private readonly IApplicationService _applicationService;

    public ProcessOCREndpoint(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    public override void Configure()
    {
        Post("api/ocr/process");
        AllowAnonymous();
        Description(b => b
            .Produces<ProcessOCRResponse>(200)
            .ProducesProblem(400)
            .WithTags("OCR"));
    }

    public override async Task HandleAsync(ProcessImageRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _applicationService.ProcessOCRAsync(req.ImagePath);
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
    public string ImagePath { get; set; } = string.Empty;
}

public class ProcessOCRResponse
{
    public string Result { get; set; } = string.Empty;
}

