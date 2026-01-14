# GPT4All Quick Start Guide

## Step-by-Step Setup for GPT4All Desktop Application

### Step 1: Install a Model

1. **Open GPT4All** (you should see "No Model Installed" message)
2. **Click the "Install a Model" button** (dark green button in the center)
3. **Select a model** from the list. Recommended models for OCR text enhancement:
   - **orca-mini-3b** - Fast, good for text correction (recommended)
   - **mistral-7b** - Better quality, requires more RAM
   - **llama-2-7b** - High quality, requires significant RAM
4. **Wait for download** - The model will download and install automatically

### Step 2: Enable API Server

1. **Click the Settings icon** (gear icon) in the left sidebar
2. **Look for "API Server" or "Server" settings**
3. **Enable the API server** (toggle it ON)
4. **Note the port number** (default is usually `4891` or `5000`)
5. **Click "Start Server" or "Enable"** if there's a button

### Step 3: Verify Server is Running

Open a web browser or PowerShell and test:

```powershell
# Test if server is running (replace port if different)
curl http://localhost:4891/health

# Or test with Invoke-WebRequest (PowerShell)
Invoke-WebRequest -Uri "http://localhost:4891/health" -Method GET
```

If you get a response (even an error), the server is running!

### Step 4: Update Your appsettings.json

Make sure your `appsettings.json` matches the port GPT4All is using:

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

**Important:** If GPT4All uses a different port (like 5000), change `BaseUrl` to match.

### Step 5: Test Your OCR Application

1. **Keep GPT4All running** (don't close it)
2. **Run your OCR application**
3. **Process a document** through your OCR endpoint
4. **Check the logs** - you should see "Successfully enhanced OCR text using GPT4All"

## Troubleshooting

### Can't Find API Server Settings?

In GPT4All v3.10.0, the API server might be:
- Under **Settings → Advanced → Enable API Server**
- Or look for a **"Server" tab** in settings
- Some versions have it under **Settings → Network**

### Server Not Starting?

1. **Check if port is already in use:**
   ```powershell
   netstat -ano | findstr :4891
   ```
2. **Try a different port** in GPT4All settings
3. **Update BaseUrl** in your appsettings.json to match

### Model Not Loading?

- Make sure you've **installed at least one model**
- **Restart GPT4All** after installing a model
- Check that the model is **selected** in the model dropdown

### Still Having Issues?

1. **Check GPT4All logs** - Look for error messages
2. **Try the Python server option** instead (see GPT4ALL_INTEGRATION.md)
3. **Verify firewall** isn't blocking localhost connections

## Alternative: Use Python Server (If Desktop App Doesn't Work)

If the desktop app API server doesn't work, you can run GPT4All as a Python server:

```bash
pip install gpt4all flask
```

Create a file `gpt4all_server.py`:

```python
from flask import Flask, request, jsonify
from gpt4all import GPT4All

app = Flask(__name__)
model = GPT4All("orca-mini-3b-gguf2-q4_0.gguf")

@app.route('/v1/completions', methods=['POST'])
def completions():
    data = request.json
    prompt = data.get('prompt', '')
    response = model.generate(
        prompt, 
        max_tokens=data.get('max_tokens', 2048),
        temp=data.get('temperature', 0.1)
    )
    return jsonify({
        'choices': [{'text': response}]
    })

@app.route('/health', methods=['GET'])
def health():
    return jsonify({'status': 'ok'})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=4891)
```

Run it:
```bash
python gpt4all_server.py
```

