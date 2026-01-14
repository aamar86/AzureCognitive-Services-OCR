# Google Gemma LLM Integration Guide (DEPRECATED)

**⚠️ This integration has been replaced with GPT4All for free, local LLM processing.**

See [GPT4ALL_INTEGRATION.md](./GPT4ALL_INTEGRATION.md) for the current implementation.

---

This document explains how to set up and use Google Gemma LLM for enhancing OCR text interpretation and structuring (legacy documentation).

## Overview

The `GemmaTextEnhancementService` uses Google's Generative AI API (which supports Gemma models) to:
- Correct OCR errors (common mistakes like '0' instead of 'O', '1' instead of 'I')
- Fix spacing issues and concatenated words
- Structure text logically while preserving important formatting
- Ensure dates, numbers, and identifiers are accurate
- Maintain all critical information from the original text

## Setup Instructions

### 1. Get Google AI API Key

1. Go to [Google AI Studio](https://makersuite.google.com/app/apikey)
2. Sign in with your Google account
3. Create a new API key
4. Copy the API key

### 2. Configure the Service

Add the following configuration to your `appsettings.json`:

```json
{
  "Gemma": {
    "Enabled": true,
    "ApiKey": "YOUR_API_KEY_HERE",
    "BaseUrl": "https://generativelanguage.googleapis.com/v1/",
    "ModelName": "gemini-1.5-flash",
    "MaxOutputTokens": 2048
  }
}
```

**Note:** The API version has been updated from `v1beta` to `v1` as v1beta is deprecated. The default model is now `gemini-1.5-flash` (faster) or you can use `gemini-1.5-pro` (better quality).

### 3. Configuration Options

- **Enabled**: Set to `false` to disable Gemma enhancement (will use original OCR text)
- **ApiKey**: Your Google AI Studio API key
- **BaseUrl**: API endpoint (defaults to Google Generative AI API)
- **ModelName**: Model to use (e.g., "gemini-1.5-flash" for speed, "gemini-1.5-pro" for quality, "gemini-2.0-flash-exp" for latest)
- **MaxOutputTokens**: Maximum length of enhanced text response

**Available Models:**
- `gemini-1.5-flash` - Fast and efficient (recommended for OCR enhancement)
- `gemini-1.5-pro` - Higher quality, slower
- `gemini-2.0-flash-exp` - Latest experimental model

### 4. Using Vertex AI (Alternative)

If you prefer to use Vertex AI instead of Google AI Studio:

1. Set up a Vertex AI project on Google Cloud
2. Enable the Generative AI API
3. Update the configuration:

```json
{
  "Gemma": {
    "Enabled": true,
    "ApiKey": "YOUR_VERTEX_AI_ACCESS_TOKEN",
    "BaseUrl": "https://us-central1-aiplatform.googleapis.com/v1/projects/YOUR_PROJECT/locations/us-central1/publishers/google/models/",
    "ModelName": "gemini-pro",
    "MaxOutputTokens": 2048
  }
}
```

## How It Works

1. **OCR Extraction**: The OCR service extracts raw text from the document image
2. **Text Enhancement**: The raw OCR text is sent to Gemma LLM with a specialized prompt
3. **Error Correction**: Gemma corrects OCR errors and structures the text
4. **Document Parsing**: The enhanced text is then parsed by `DocumentParsingService`

## Integration Points

The service is integrated into the `ApplicationService` workflow:

```csharp
// Extract text first
var rawText = await _ocrService.ExtractTextAsync(filePath, documentType);

// Enhance text using Gemma LLM if available
var enhancedText = _textEnhancementService != null
    ? await _textEnhancementService.EnhanceTextAsync(rawText, documentType)
    : rawText;

// Parse the enhanced text
return _documentParsingService.Parse(enhancedText, documentType);
```

## Error Handling

The service is designed to be resilient:
- If the API call fails, it returns the original OCR text
- If the service is disabled or not configured, it returns the original text
- All errors are logged but don't interrupt the OCR processing flow

## Cost Considerations

- Google AI Studio offers free tier with rate limits
- Vertex AI charges based on API usage
- Monitor your usage in the Google Cloud Console

## Testing

To test the integration:

1. Ensure `Gemma.Enabled` is set to `true` in `appsettings.json`
2. Provide a valid `ApiKey`
3. Process a document through the OCR endpoint
4. Check logs to see if text enhancement occurred
5. Compare the `RawText` and parsed results to see improvements

## Troubleshooting

### Service Not Working

1. Check that `Enabled` is set to `true`
2. Verify the API key is correct
3. Check network connectivity to Google APIs
4. Review application logs for error messages

### API Errors

- **401 Unauthorized**: Invalid API key
- **403 Forbidden**: API not enabled or quota exceeded
- **404 Not Found**: Model not found - check that you're using v1 API and a valid model name (e.g., "gemini-1.5-flash")
- **429 Too Many Requests**: Rate limit exceeded

**Common Fix for 404 Error:**
If you get a 404 error saying the model is not found:
1. Ensure `BaseUrl` uses `/v1/` not `/v1beta/`
2. Use a current model name like `gemini-1.5-flash` or `gemini-1.5-pro`
3. Check available models using the `ListAvailableModelsAsync` method

### Performance

- The enhancement adds latency (typically 1-3 seconds per document)
- Consider caching enhanced text for repeated processing
- Use async/await to avoid blocking the request pipeline

## Future Enhancements

Potential improvements:
- Caching enhanced text to reduce API calls
- Batch processing multiple documents
- Custom prompts per document type
- Fallback to multiple LLM providers
- Local Gemma model deployment option

