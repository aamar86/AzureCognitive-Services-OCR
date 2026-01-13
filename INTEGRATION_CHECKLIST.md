# Integration Checklist

Use this checklist to track your integration progress.

## Pre-Integration
- [ ] Identify target project structure (Application/Infrastructure/API layers)
- [ ] Ensure .NET 8.0+ is installed
- [ ] Download Tesseract language data files (eng.traineddata)

## Step 1: Copy Files
- [ ] Copy `IOCRService.cs` to Application layer
- [ ] Copy `IDocumentParsingService.cs` to Application layer
- [ ] Copy `IDocumentTypeDetectionService.cs` to Application layer
- [ ] Copy `ApplicationService.cs` to Application layer
- [ ] Copy all DTOs (`OcrResult.cs`, `PassportResult.cs`, `EmiratesIdResult.cs`, `UAETradeLicenseResult.cs`) to Application/Dtos
- [ ] Copy `InvalidDocumentTypeException.cs` to Application/Exceptions
- [ ] Copy `TesseractOCRService.cs` to Infrastructure layer
- [ ] Copy `DocumentParsingService.cs` to Infrastructure layer
- [ ] Copy `DocumentTypeDetectionService.cs` to Infrastructure layer
- [ ] Copy `tessdata` folder with `eng.traineddata` to API/Web project

## Step 2: Install NuGet Packages
- [ ] Install `Tesseract` (v5.2.0) in Infrastructure project
- [ ] Install `System.Drawing.Common` (v8.0.0) in Infrastructure project
- [ ] Install `PdfPig` (v0.1.8) in Infrastructure project
- [ ] Install `PdfiumViewer` (v2.13.0) in Infrastructure project
- [ ] Install `PdfiumViewer.Native.x86_64.v8-xfa` (v2018.4.8.256) in Infrastructure project
- [ ] Install `Microsoft.Extensions.Options` (v8.0.2) in Infrastructure project
- [ ] Install `Microsoft.Extensions.Logging.Abstractions` (v8.0.0) in Infrastructure project

## Step 3: Update Namespaces
- [ ] Replace `CleanArchitecture.OCR.Application` with your Application namespace
- [ ] Replace `CleanArchitecture.OCR.Infrastructure` with your Infrastructure namespace
- [ ] Update all `using` statements in copied files
- [ ] Verify no namespace errors remain

## Step 4: Register Services
- [ ] Add `TesseractOCRSettings` configuration binding in `Program.cs` or `Startup.cs`
- [ ] Register `IOCRService` → `TesseractOCRService` in DI container
- [ ] Register `IDocumentParsingService` → `DocumentParsingService` in DI container
- [ ] Register `IDocumentTypeDetectionService` → `DocumentTypeDetectionService` in DI container
- [ ] Register `IApplicationService` → `ApplicationService` in DI container

## Step 5: Configuration
- [ ] Add `TesseractOCR` section to `appsettings.json`
- [ ] Set `TessDataPath` in configuration (relative or absolute path)
- [ ] Set `Language` in configuration (default: "eng")
- [ ] Add configuration to `appsettings.Development.json` if needed

## Step 6: Ensure Files Are Copied
- [ ] Verify `tessdata` folder is accessible at runtime
- [ ] Add `tessdata` to `.csproj` with `CopyToOutputDirectory` if needed
- [ ] Test that `eng.traineddata` file exists in output directory

## Step 7: Testing
- [ ] Test OCR with a sample image (PNG/JPG)
- [ ] Test OCR with a sample PDF
- [ ] Test Passport document processing
- [ ] Test Emirates ID document processing
- [ ] Test UAE Trade License document processing
- [ ] Test error handling (invalid file, missing file, etc.)
- [ ] Test document type detection

## Step 8: Integration Complete
- [ ] All tests pass
- [ ] No compilation errors
- [ ] No runtime errors
- [ ] Documentation updated (if needed)
- [ ] Code reviewed

## Quick Commands Reference

### Install Packages (PowerShell)
```powershell
cd YourProject.Infrastructure
Install-Package Tesseract -Version 5.2.0
Install-Package System.Drawing.Common -Version 8.0.0
Install-Package PdfPig -Version 0.1.8
Install-Package PdfiumViewer -Version 2.13.0
Install-Package PdfiumViewer.Native.x86_64.v8-xfa -Version 2018.4.8.256
Install-Package Microsoft.Extensions.Options -Version 8.0.2
Install-Package Microsoft.Extensions.Logging.Abstractions -Version 8.0.0
```

### Install Packages (.NET CLI)
```bash
cd YourProject.Infrastructure
dotnet add package Tesseract --version 5.2.0
dotnet add package System.Drawing.Common --version 8.0.0
dotnet add package PdfPig --version 0.1.8
dotnet add package PdfiumViewer --version 2.13.0
dotnet add package PdfiumViewer.Native.x86_64.v8-xfa --version 2018.4.8.256
dotnet add package Microsoft.Extensions.Options --version 8.0.2
dotnet add package Microsoft.Extensions.Logging.Abstractions --version 8.0.0
```

### Restore and Build
```bash
dotnet restore
dotnet build
```

### Configuration Template
```json
{
  "TesseractOCR": {
    "TessDataPath": "tessdata",
    "Language": "eng"
  }
}
```


