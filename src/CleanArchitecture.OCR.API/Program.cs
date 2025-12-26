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

// Register application services
builder.Services.AddScoped<IOCRService, OCRService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseFastEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

app.Run();
