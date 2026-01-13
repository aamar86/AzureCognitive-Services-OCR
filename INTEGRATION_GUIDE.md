# Integration Guide: Tesseract OCR Service with Document Types

This guide will help you integrate the Tesseract OCR service with support for Passport, Emirates ID, and UAE Trade License document types into your existing project.

## Table of Contents
1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Step-by-Step Integration](#step-by-step-integration)
4. [Configuration](#configuration)
5. [Usage Examples](#usage-examples)
6. [Troubleshooting](#troubleshooting)

---

## Overview

The OCR service consists of three main layers:
- **Application Layer**: Interfaces, DTOs, Enums, and Business Logic
- **Infrastructure Layer**: Tesseract OCR implementation, Document Parsing, and Type Detection
- **API Layer**: Endpoints (optional, if you need REST API)

### Supported Document Types
- **Passport**: Extracts MRZ data (passport number, name, DOB, expiry, etc.)
- **Emirates ID**: Extracts ID number, name, nationality, DOB, expiry date
- **UAE Trade License**: Extracts license number, company name, expiry date, activity, etc.

---

## Prerequisites

1. **.NET 8.0** or later
2. **Tesseract Language Data Files** (tessdata folder with `eng.traineddata`)
3. **Visual Studio 2022** or **VS Code** with C# extension

---

## Step-by-Step Integration

### Step 1: Copy Required Files

#### 1.1 Application Layer Files
Copy these files to your Application/Contracts project:

```
📁 YourProject.Application/
  ├── 📄 IOCRService.cs
  ├── 📄 IDocumentParsingService.cs
  ├── 📄 IDocumentTypeDetectionService.cs
  ├── 📄 ApplicationService.cs (or rename to your naming convention)
  ├── 📁 Dtos/
  │   ├── OcrResult.cs
  │   ├── PassportResult.cs
  │   ├── EmiratesIdResult.cs
  │   └── UAETradeLicenseResult.cs
  ├── 📁 Exceptions/
  │   └── InvalidDocumentTypeException.cs
```

**Key Files:**
- `IOCRService.cs` - Interface for OCR text extraction
- `IDocumentParsingService.cs` - Interface for parsing extracted text
- `IDocumentTypeDetectionService.cs` - Interface for detecting document type
- `ApplicationService.cs` - Main service orchestrating OCR, detection, and parsing

#### 1.2 Infrastructure Layer Files
Copy these files to your Infrastructure project:

```
📁 YourProject.Infrastructure/
  ├── 📄 TesseractOCRService.cs
  ├── 📄 DocumentParsingService.cs
  └── 📄 DocumentTypeDetectionService.cs
```

**Key Files:**
- `TesseractOCRService.cs` - Tesseract OCR implementation (handles PDF and images)
- `DocumentParsingService.cs` - Parses extracted text into structured data
- `DocumentTypeDetectionService.cs` - Detects document type from raw text

#### 1.3 Copy Tesseract Data Files
Copy the `tessdata` folder to your API/Web project root (or a location accessible at runtime):

```
📁 YourProject.API/
  └── 📁 tessdata/
      └── 📄 eng.traineddata
```

**Note:** You can download additional language files from: https://github.com/tesseract-ocr/tessdata

---

### Step 2: Add NuGet Packages

Add these NuGet packages to your **Infrastructure** project:

```xml
<PackageReference Include="Tesseract" Version="5.2.0" />
<PackageReference Include="System.Drawing.Common" Version="8.0.0" />
<PackageReference Include="PdfPig" Version="0.1.8" />
<PackageReference Include="PdfiumViewer" Version="2.13.0" />
<PackageReference Include="PdfiumViewer.Native.x86_64.v8-xfa" Version="2018.4.8.256" />
<PackageReference Include="Microsoft.Extensions.Options" Version="8.0.2" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
```

**Using Package Manager Console:**
```powershell
Install-Package Tesseract -Version 5.2.0
Install-Package System.Drawing.Common -Version 8.0.0
Install-Package PdfPig -Version 0.1.8
Install-Package PdfiumViewer -Version 2.13.0
Install-Package PdfiumViewer.Native.x86_64.v8-xfa -Version 2018.4.8.256
Install-Package Microsoft.Extensions.Options -Version 8.0.2
Install-Package Microsoft.Extensions.Logging.Abstractions -Version 8.0.0
```

**Using .NET CLI:**
```bash
dotnet add package Tesseract --version 5.2.0
dotnet add package System.Drawing.Common --version 8.0.0
dotnet add package PdfPig --version 0.1.8
dotnet add package PdfiumViewer --version 2.13.0
dotnet add package PdfiumViewer.Native.x86_64.v8-xfa --version 2018.4.8.256
dotnet add package Microsoft.Extensions.Options --version 8.0.2
dotnet add package Microsoft.Extensions.Logging.Abstractions --version 8.0.0
```

---

### Step 3: Update Namespaces

After copying files, update the namespaces to match your project structure:

**Example:**
- Change `CleanArchitecture.OCR.Application` → `YourProject.Application`
- Change `CleanArchitecture.OCR.Infrastructure` → `YourProject.Infrastructure`

**Quick Find & Replace:**
1. In Visual Studio: `Ctrl+Shift+H` (Find and Replace in Files)
2. Find: `CleanArchitecture.OCR.Application`
3. Replace: `YourProject.Application`
4. Repeat for `CleanArchitecture.OCR.Infrastructure`

---

### Step 4: Register Services in DI Container

In your `Program.cs` or `Startup.cs`, register the services:

```csharp
using YourProject.Application;
using YourProject.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure Tesseract OCR settings
builder.Services.Configure<TesseractOCRSettings>(
    builder.Configuration.GetSection(TesseractOCRSettings.SectionName));

// Register OCR services
builder.Services.AddScoped<IOCRService, TesseractOCRService>();
builder.Services.AddScoped<IDocumentParsingService, DocumentParsingService>();
builder.Services.AddScoped<IDocumentTypeDetectionService, DocumentTypeDetectionService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();

var app = builder.Build();
```

---

### Step 5: Add Configuration

Add Tesseract OCR settings to your `appsettings.json`:

```json
{
  "TesseractOCR": {
    "TessDataPath": "tessdata",
    "Language": "eng"
  }
}
```

**Configuration Options:**
- `TessDataPath`: Path to tessdata folder (relative to app root or absolute path)
- `Language`: Tesseract language code (e.g., "eng", "ara", "fra")

**For Development (`appsettings.Development.json`):**
```json
{
  "TesseractOCR": {
    "TessDataPath": "C:\\Path\\To\\Your\\Project\\tessdata",
    "Language": "eng"
  }
}
```

---

### Step 6: Ensure Tesseract Data Files Are Copied

Make sure the `tessdata` folder is copied to the output directory:

**Option A: Copy manually to output directory**

**Option B: Add to `.csproj` file:**
```xml
<ItemGroup>
  <None Include="tessdata\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

## Configuration

### TesseractOCRSettings Class

The `TesseractOCRSettings` class is defined in `TesseractOCRService.cs`:

```csharp
public class TesseractOCRSettings
{
    public const string SectionName = "TesseractOCR";
    
    public string? TessDataPath { get; set; }
    public string Language { get; set; } = "eng";
}
```

### Supported File Formats

- **Images**: PNG, JPG, JPEG, BMP, TIFF, GIF
- **Documents**: PDF (text-based and image-based/scanned)

---

## Usage Examples

### Example 1: Basic OCR Processing (Auto-detect Document Type)

```csharp
using YourProject.Application;

public class MyService
{
    private readonly IApplicationService _ocrService;

    public MyService(IApplicationService ocrService)
    {
        _ocrService = ocrService;
    }

    public async Task<OcrResult> ProcessDocument(string filePath)
    {
        // Extract text and auto-detect document type
        var rawText = await _ocrService.ProcessOCRAsync(filePath);
        
        // Or specify document type explicitly
        var result = await _ocrService.ProcessOCRAsync(
            filePath, 
            DocumentType.Passport
        );
        
        return result;
    }
}
```

### Example 2: Process Specific Document Type

```csharp
// Process Emirates ID
var emiratesIdResult = await _ocrService.ProcessOCRAsync(
    filePath, 
    DocumentType.EmiratesID
);

if (emiratesIdResult.IsValid && emiratesIdResult.EmiratesId != null)
{
    var idNumber = emiratesIdResult.EmiratesId.IdNumber;
    var fullName = emiratesIdResult.EmiratesId.FullName;
    var dob = emiratesIdResult.EmiratesId.DateOfBirth;
    // ... use extracted data
}

// Process Passport
var passportResult = await _ocrService.ProcessOCRAsync(
    filePath, 
    DocumentType.Passport
);

if (passportResult.IsValid && passportResult.Passport != null)
{
    var passportNumber = passportResult.Passport.PassportNumber;
    var surname = passportResult.Passport.Surname;
    // ... use extracted data
}

// Process UAE Trade License
var tradeLicenseResult = await _ocrService.ProcessOCRAsync(
    filePath, 
    DocumentType.UAETradeLicense
);

if (tradeLicenseResult.IsValid && tradeLicenseResult.UAETradeLicense != null)
{
    var licenseNumber = tradeLicenseResult.UAETradeLicense.TradeLicenseNumber;
    var companyName = tradeLicenseResult.UAETradeLicense.CompanyName;
    // ... use extracted data
}
```

### Example 3: Direct OCR Text Extraction (Without Parsing)

```csharp
using YourProject.Application;

public class MyService
{
    private readonly IOCRService _ocrService;

    public MyService(IOCRService ocrService)
    {
        _ocrService = ocrService;
    }

    public async Task<string> ExtractText(string filePath)
    {
        // Extract raw text only
        var rawText = await _ocrService.ExtractTextAsync(filePath);
        return rawText;
    }

    public async Task<string> ExtractTextForDocumentType(
        string filePath, 
        DocumentType documentType)
    {
        // Extract text with document-specific OCR configuration
        var rawText = await _ocrService.ExtractTextAsync(filePath, documentType);
        return rawText;
    }
}
```

### Example 4: Document Type Detection

```csharp
using YourProject.Application;

public class MyService
{
    private readonly IDocumentTypeDetectionService _detectionService;
    private readonly IOCRService _ocrService;

    public MyService(
        IDocumentTypeDetectionService detectionService,
        IOCRService ocrService)
    {
        _detectionService = detectionService;
        _ocrService = ocrService;
    }

    public async Task<DocumentType> DetectDocumentType(string filePath)
    {
        // First extract text
        var rawText = await _ocrService.ExtractTextAsync(filePath);
        
        // Then detect document type
        var documentType = _detectionService.DetectDocumentType(rawText);
        
        return documentType;
    }
}
```

### Example 5: Manual Parsing

```csharp
using YourProject.Application;

public class MyService
{
    private readonly IDocumentParsingService _parsingService;
    private readonly IOCRService _ocrService;

    public MyService(
        IDocumentParsingService parsingService,
        IOCRService ocrService)
    {
        _parsingService = parsingService;
        _ocrService = ocrService;
    }

    public async Task<OcrResult> ProcessManually(string filePath)
    {
        // Extract text
        var rawText = await _ocrService.ExtractTextAsync(filePath);
        
        // Parse manually (you specify the document type)
        var result = _parsingService.Parse(rawText, DocumentType.Passport);
        
        return result;
    }
}
```

---

## Troubleshooting

### Issue 1: Tesseract Data Directory Not Found

**Error:** `DirectoryNotFoundException: Tesseract data directory not found`

**Solution:**
1. Ensure `tessdata` folder exists in your project
2. Check `TessDataPath` in `appsettings.json` points to the correct location
3. Verify `eng.traineddata` file exists in the tessdata folder
4. Ensure tessdata folder is copied to output directory

### Issue 2: PDF Processing Fails

**Error:** PDF cannot be processed

**Solution:**
1. Ensure `PdfiumViewer` and `PdfiumViewer.Native.x86_64.v8-xfa` packages are installed
2. For Linux/Mac, you may need different native packages
3. Check if PDF is password-protected or corrupted

### Issue 3: Poor OCR Accuracy

**Solutions:**
1. Use higher resolution images (300 DPI recommended)
2. Ensure good image quality (clear, well-lit, no blur)
3. Pre-process images (deskew, enhance contrast)
4. Use appropriate language data files
5. Adjust Tesseract page segmentation mode in `TesseractOCRService.cs`

### Issue 4: Document Type Not Detected Correctly

**Solutions:**
1. Ensure document image quality is good
2. Check if OCR extracted text contains expected keywords
3. Review `DocumentTypeDetectionService.cs` patterns
4. Manually specify document type if auto-detection fails

### Issue 5: Namespace Errors After Copying

**Solution:**
1. Update all namespace references to match your project structure
2. Use Find & Replace in Visual Studio
3. Ensure `using` statements are correct

### Issue 6: Missing Dependencies

**Error:** `The type or namespace name 'X' could not be found`

**Solution:**
1. Verify all NuGet packages are installed
2. Check project references are correct
3. Restore NuGet packages: `dotnet restore`
4. Rebuild solution

---

## Project Structure Summary

After integration, your project should have:

```
YourProject/
├── YourProject.Application/
│   ├── IOCRService.cs
│   ├── IDocumentParsingService.cs
│   ├── IDocumentTypeDetectionService.cs
│   ├── ApplicationService.cs
│   ├── Dtos/
│   │   ├── OcrResult.cs
│   │   ├── PassportResult.cs
│   │   ├── EmiratesIdResult.cs
│   │   └── UAETradeLicenseResult.cs
│   └── Exceptions/
│       └── InvalidDocumentTypeException.cs
│
├── YourProject.Infrastructure/
│   ├── TesseractOCRService.cs
│   ├── DocumentParsingService.cs
│   └── DocumentTypeDetectionService.cs
│
└── YourProject.API/ (or Web project)
    ├── tessdata/
    │   └── eng.traineddata
    └── appsettings.json
```

---

## Additional Notes

1. **Performance**: OCR processing can be CPU-intensive. Consider using async/await and background processing for large files.

2. **Error Handling**: The service throws exceptions for invalid files, missing configurations, etc. Wrap calls in try-catch blocks.

3. **Logging**: The `TesseractOCRService` supports logging. Ensure `ILogger<TesseractOCRService>` is registered in DI.

4. **Thread Safety**: Services are designed to be thread-safe when used with dependency injection (scoped lifetime).

5. **Customization**: You can extend `DocumentParsingService` to add more document types or improve parsing logic.

---

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review the source code comments
3. Verify all dependencies are correctly installed
4. Ensure configuration is correct

---

## License

Ensure you comply with the licenses of all dependencies:
- Tesseract OCR: Apache License 2.0
- PdfPig: Apache License 2.0
- PdfiumViewer: Apache License 2.0


