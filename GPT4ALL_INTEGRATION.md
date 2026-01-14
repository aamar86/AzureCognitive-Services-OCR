# GPT4All LLM Integration Guide

This document explains how to set up and use GPT4All (free, local LLM) for enhancing OCR text interpretation and structuring.

## Overview

The `GPT4AllTextEnhancementService` uses GPT4All, a free and open-source local LLM, to:
- Correct OCR errors (common mistakes like '0' instead of 'O', '1' instead of 'I')
- Fix spacing issues and concatenated words
- Structure text logically while preserving important formatting
- Ensure dates, numbers, and identifiers are accurate
- Maintain all critical information from the original text

**Key Advantages:**
- ✅ **100% Free** - No API costs
- ✅ **Privacy** - All processing happens locally
- ✅ **No Internet Required** - Works offline
- ✅ **No Rate Limits** - Process as many documents as needed

## Setup Instructions

### 1. Install GPT4All Server

You need to run GPT4All as a local server. Choose one of these options:

#### Option A: GPT4All Desktop Application (Easiest)

1. Download GPT4All from [https://gpt4all.io](https://gpt4all.io)
2. Install and run the application
3. Enable the API server in settings (usually runs on `http://localhost:4891`)

#### Option B: GPT4All Python Server

1. Install Python 3.8+
2. Install GPT4All:
   ```bash
   pip install gpt4all
   ```
3. Run the server:
   ```python
   from gpt4all import GPT4All
   
   # Start server
   model = GPT4All("orca-mini-3b-gguf2-q4_0.gguf")
   # Or use the API server mode
   ```

#### Option C: GPT4All C++ Server

1. Build GPT4All from source or use pre-built binaries
2. Run with API server enabled:
   ```bash
   ./gpt4all --server --port 4891
   ```

### 2. Configure the Service

Add the following configuration to your `appsettings.json`:

```json
{
  "GPT4All": {
    "Enabled": true,
    "BaseUrl": "http://localhost:4891",
    "Endpoint": "/v1/completions",
    "HealthEndpoint": "/health",
    "MaxTokens": 2048,
    "Temperature": 0.1,
    "TopP": 0.9,
    "TopK": 40,
    "RepeatPenalty": 1.1,
    "TimeoutSeconds": 120
  }
}
```

### 3. Configuration Options

- **Enabled**: Set to `false` to disable GPT4All enhancement (will use original OCR text)
- **BaseUrl**: URL of your local GPT4All server (default: `http://localhost:4891`)
- **Endpoint**: API endpoint path (default: `/v1/completions` or `/api/generate` depending on GPT4All version)
- **HealthEndpoint**: Health check endpoint (default: `/health`)
- **MaxTokens**: Maximum length of enhanced text response
- **Temperature**: Lower values (0.1) = more deterministic, Higher values (0.7) = more creative
- **TopP**: Nucleus sampling parameter
- **TopK**: Top-k sampling parameter
- **RepeatPenalty**: Penalty for repeating tokens (1.0 = no penalty)
- **TimeoutSeconds**: Request timeout in seconds

### 4. Verify GPT4All Server is Running

Before using the service, ensure GPT4All server is running:

```bash
# Test if server is accessible
curl http://localhost:4891/health
```

Or check in your application logs - the service will log warnings if it can't connect.

## How It Works

1. **OCR Extraction**: The OCR service extracts raw text from the document image
2. **Text Enhancement**: The raw OCR text is sent to your local GPT4All server with a specialized prompt
3. **Error Correction**: GPT4All corrects OCR errors and structures the text locally
4. **Document Parsing**: The enhanced text is then parsed by `DocumentParsingService`

## Integration Points

The service is integrated into the `ApplicationService` workflow:

```csharp
// Extract text first
var rawText = await _ocrService.ExtractTextAsync(filePath, documentType);

// Enhance text using GPT4All LLM if available
var enhancedText = _textEnhancementService != null
    ? await _textEnhancementService.EnhanceTextAsync(rawText, documentType)
    : rawText;

// Parse the enhanced text
return _documentParsingService.Parse(enhanced text, documentType);
```

## Error Handling

The service is designed to be resilient:
- If the API call fails, it returns the original OCR text
- If the service is disabled or not configured, it returns the original text
- All errors are logged but don't interrupt the OCR processing flow
- Includes a health check method to verify server availability

## Performance Considerations

- **First Request**: May be slower as GPT4All loads the model into memory
- **Subsequent Requests**: Faster once the model is loaded
- **Model Size**: Larger models provide better quality but require more RAM
- **Recommended Models**: 
  - `orca-mini-3b` - Fast, good for OCR correction
  - `mistral-7b` - Better quality, requires more RAM
  - `llama-2-7b` - High quality, requires significant RAM

## Troubleshooting

### Service Not Working

1. **Check GPT4All Server**: Ensure GPT4All is running and accessible
   ```bash
   curl http://localhost:4891/health
   ```

2. **Verify Configuration**: Check that `BaseUrl` matches your GPT4All server address

3. **Check Port**: Default is 4891, but GPT4All might use a different port

4. **Review Logs**: Check application logs for connection errors

### Connection Errors

- **Connection Refused**: GPT4All server is not running
- **Timeout**: Server is too slow or model is too large - increase `TimeoutSeconds`
- **404 Not Found**: Wrong endpoint path - check GPT4All API documentation

### Performance Issues

- **Slow Processing**: 
  - Use a smaller model (e.g., `orca-mini-3b`)
  - Reduce `MaxTokens` if documents are short
  - Ensure sufficient RAM is available

- **Out of Memory**:
  - Use a smaller model
  - Close other applications
  - Consider using a model with quantization (Q4, Q5)

### API Endpoint Variations

Different GPT4All versions may use different endpoints:
- `/v1/completions` - OpenAI-compatible format
- `/api/generate` - Native GPT4All format
- `/chat/completions` - Chat format

Check your GPT4All version documentation and update the `Endpoint` configuration accordingly.

## Testing

To test the integration:

1. Ensure GPT4All server is running on `http://localhost:4891`
2. Set `GPT4All.Enabled` to `true` in `appsettings.json`
3. Process a document through the OCR endpoint
4. Check logs to see if text enhancement occurred
5. Compare the `RawText` and parsed results to see improvements

## Example GPT4All Server Setup (Python)

If you want to run GPT4All as a Python server:

```python
from flask import Flask, request, jsonify
from gpt4all import GPT4All

app = Flask(__name__)
model = GPT4All("orca-mini-3b-gguf2-q4_0.gguf")

@app.route('/v1/completions', methods=['POST'])
def completions():
    data = request.json
    prompt = data.get('prompt', '')
    response = model.generate(prompt, max_tokens=data.get('max_tokens', 2048))
    return jsonify({
        'choices': [{'text': response}]
    })

@app.route('/health', methods=['GET'])
def health():
    return jsonify({'status': 'ok'})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=4891)
```

## Security Notes

- GPT4All runs locally, so your data never leaves your machine
- No API keys or authentication required
- Consider firewall rules if running on a network
- For production, consider running GPT4All in a containerized environment

## Future Enhancements

Potential improvements:
- Automatic model download and setup
- Model selection based on document complexity
- Batch processing optimization
- Caching enhanced text to reduce processing
- Support for multiple GPT4All instances (load balancing)

