using CleanArchitecture.OCR.Application;
using CleanArchitecture.OCR.Infrastructure;
using FastEndpoints;
using FastEndpoints.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "Clean Architecture OCR API";
        s.Version = "v1";
    };
});

// Configure Azure OCR settings
builder.Services.Configure<AzureOCRSettings>(
    builder.Configuration.GetSection(AzureOCRSettings.SectionName));

// Configure Tesseract OCR settings
builder.Services.Configure<TesseractOCRSettings>(
    builder.Configuration.GetSection(TesseractOCRSettings.SectionName));

// Configure GPT4All text enhancement settings
builder.Services.Configure<GPT4AllSettings>(
    builder.Configuration.GetSection(GPT4AllSettings.SectionName));

// Register HTTP client for GPT4All API
builder.Services.AddHttpClient<GPT4AllTextEnhancementService>();

// Register application services
// Uncomment the service you want to use:
// builder.Services.AddScoped<IOCRService, OCRService>(); // Azure OCR
builder.Services.AddScoped<IOCRService, TesseractOCRService>(); // Tesseract OCR
builder.Services.AddScoped<IDocumentParsingService, DocumentParsingService>();
builder.Services.AddScoped<IDocumentTypeDetectionService, DocumentTypeDetectionService>();
builder.Services.AddScoped<ITextEnhancementService, GPT4AllTextEnhancementService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseFastEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

app.Run();
