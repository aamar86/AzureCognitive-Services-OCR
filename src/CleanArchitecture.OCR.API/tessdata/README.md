# Tesseract Language Data Files

Place your Tesseract language data files (.traineddata) in this folder.

## Required Files

For basic English OCR:
- `eng.traineddata` - English language data

For Arabic/Emirates ID support:
- `ara.traineddata` - Arabic language data
- `eng.traineddata` - English language data (for mixed content)

## How to Download

1. Go to: https://github.com/tesseract-ocr/tessdata
2. Click on the `.traineddata` file you need (e.g., `eng.traineddata`)
3. Click "Download" or "Raw" to download the file
4. Place the downloaded `.traineddata` file(s) directly in this `tessdata` folder

## Example Structure

```
tessdata/
  ├── eng.traineddata
  ├── ara.traineddata
  └── README.md (this file)
```

## Notes

- These files will be automatically copied to the output directory during build
- The minimum file size for `eng.traineddata` is approximately 4-5 MB
- For passports and Emirates ID, you may want both English and Arabic language files

