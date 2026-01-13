# Files to Copy - Quick Reference

This document lists all files that need to be copied from this project to your target project.

## 📁 Application Layer Files

Copy these files to your **Application/Contracts** project:

### Interfaces
```
src/CleanArchitecture.OCR.Application/
├── IOCRService.cs                    → YourProject.Application/IOCRService.cs
├── IDocumentParsingService.cs       → YourProject.Application/IDocumentParsingService.cs
└── IDocumentTypeDetectionService.cs → YourProject.Application/IDocumentTypeDetectionService.cs
```

### Services
```
src/CleanArchitecture.OCR.Application/
└── ApplicationService.cs            → YourProject.Application/ApplicationService.cs
```

### DTOs (Data Transfer Objects)
```
src/CleanArchitecture.OCR.Application/Dtos/
├── OcrResult.cs                     → YourProject.Application/Dtos/OcrResult.cs
├── PassportResult.cs                → YourProject.Application/Dtos/PassportResult.cs
├── EmiratesIdResult.cs              → YourProject.Application/Dtos/EmiratesIdResult.cs
└── UAETradeLicenseResult.cs         → YourProject.Application/Dtos/UAETradeLicenseResult.cs
```

### Exceptions
```
src/CleanArchitecture.OCR.Application/Exceptions/
└── InvalidDocumentTypeException.cs  → YourProject.Application/Exceptions/InvalidDocumentTypeException.cs
```

### Enums (in IOCRService.cs)
The `DocumentType` enum is defined in `IOCRService.cs`. Make sure it's copied:
```csharp
public enum DocumentType
{
    Passport,
    EmiratesID,
    UAETradeLicense
}
```

---

## 📁 Infrastructure Layer Files

Copy these files to your **Infrastructure** project:

```
src/CleanArchitecture.OCR.Infrastructure/
├── TesseractOCRService.cs           → YourProject.Infrastructure/TesseractOCRService.cs
├── DocumentParsingService.cs        → YourProject.Infrastructure/DocumentParsingService.cs
└── DocumentTypeDetectionService.cs   → YourProject.Infrastructure/DocumentTypeDetectionService.cs
```

**Note:** `TesseractOCRService.cs` also contains the `TesseractOCRSettings` class used for configuration.

---

## 📁 Tesseract Data Files

Copy the entire `tessdata` folder to your **API/Web** project root:

```
src/CleanArchitecture.OCR.API/tessdata/
└── eng.traineddata                   → YourProject.API/tessdata/eng.traineddata
```

**Important:** 
- The `tessdata` folder must be accessible at runtime
- Ensure it's copied to the output directory
- You can download additional language files from: https://github.com/tesseract-ocr/tessdata

---

## 📋 File Dependencies Map

### TesseractOCRService.cs depends on:
- `IOCRService` (Application layer)
- `TesseractOCRSettings` (defined in same file)
- `DocumentType` enum (Application layer)
- NuGet packages: Tesseract, System.Drawing.Common, PdfPig, PdfiumViewer

### DocumentParsingService.cs depends on:
- `IDocumentParsingService` (Application layer)
- `DocumentType` enum (Application layer)
- All DTO classes (OcrResult, PassportResult, EmiratesIdResult, UAETradeLicenseResult)

### DocumentTypeDetectionService.cs depends on:
- `IDocumentTypeDetectionService` (Application layer)
- `DocumentType` enum (Application layer)

### ApplicationService.cs depends on:
- `IApplicationService` (defined in same file)
- `IOCRService` (Application layer)
- `IDocumentParsingService` (Application layer)
- `IDocumentTypeDetectionService` (Application layer)
- `DocumentType` enum (Application layer)
- `InvalidDocumentTypeException` (Application layer)

---

## 🔄 Namespace Updates Required

After copying, update namespaces in all files:

| Old Namespace | New Namespace (Example) |
|--------------|------------------------|
| `CleanArchitecture.OCR.Application` | `YourProject.Application` |
| `CleanArchitecture.OCR.Infrastructure` | `YourProject.Infrastructure` |
| `CleanArchitecture.OCR.Application.Exceptions` | `YourProject.Application.Exceptions` |

---

## 📦 NuGet Packages Required

Install these packages in your **Infrastructure** project:

| Package | Version | Purpose |
|---------|---------|---------|
| Tesseract | 5.2.0 | OCR engine |
| System.Drawing.Common | 8.0.0 | Image processing |
| PdfPig | 0.1.8 | PDF text extraction |
| PdfiumViewer | 2.13.0 | PDF to image conversion |
| PdfiumViewer.Native.x86_64.v8-xfa | 2018.4.8.256 | Native PDF library |
| Microsoft.Extensions.Options | 8.0.2 | Configuration |
| Microsoft.Extensions.Logging.Abstractions | 8.0.0 | Logging |

---

## ✅ Copy Verification Checklist

After copying files, verify:

- [ ] All Application layer files copied
- [ ] All Infrastructure layer files copied
- [ ] tessdata folder copied with eng.traineddata
- [ ] All namespaces updated
- [ ] All NuGet packages installed
- [ ] Project references correct
- [ ] No compilation errors
- [ ] Configuration added to appsettings.json
- [ ] Services registered in DI container

---

## 🚀 Quick Copy Script (Manual Steps)

1. **Create folder structure in target project:**
   ```
   YourProject.Application/Dtos/
   YourProject.Application/Exceptions/
   YourProject.Infrastructure/
   YourProject.API/tessdata/
   ```

2. **Copy Application files:**
   - Copy all files from `src/CleanArchitecture.OCR.Application/` to `YourProject.Application/`
   - Copy DTOs to `YourProject.Application/Dtos/`
   - Copy Exceptions to `YourProject.Application/Exceptions/`

3. **Copy Infrastructure files:**
   - Copy all files from `src/CleanArchitecture.OCR.Infrastructure/` to `YourProject.Infrastructure/`

4. **Copy tessdata:**
   - Copy entire `tessdata` folder to `YourProject.API/tessdata/`

5. **Update namespaces:**
   - Use Find & Replace in Visual Studio to update all namespaces

6. **Install packages:**
   - Run NuGet package installation commands

7. **Register services:**
   - Add service registrations in Program.cs or Startup.cs

8. **Add configuration:**
   - Add TesseractOCR section to appsettings.json


