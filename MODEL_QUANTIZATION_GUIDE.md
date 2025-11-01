# ONNX Model Quantization and Local Deployment Guide

## Quick Reference Card

**⚡ TL;DR - Fastest Path to Success:**

```powershell
# 1. Setup (one-time)
mkdir C:\Temp\ModelQuantizationTest
cd C:\Temp\ModelQuantizationTest
python -m venv venv
.\venv\Scripts\Activate.ps1
pip install optimum[exporters,onnxruntime]

# 2. Quantize models
mkdir models\sentiment, models\emotion
optimum-cli export onnx --model lxyuan/distilbert-base-multilingual-cased-sentiments-student --task text-classification --optimize O3 --quantize models\sentiment\
optimum-cli export onnx --model SamLowe/roberta-base-go_emotions --task text-classification --optimize O3 --quantize models\emotion\

# 3. Test (create test.html from guide, then)
python -m http.server 8000
# Open http://localhost:8000/test.html

# 4. Copy to Sivar.Os (if tests pass)
xcopy /E /I models\sentiment C:\Users\joche\source\repos\SivarOs\Sivar.Os\Sivar.Os\wwwroot\models\sentiment
xcopy /E /I models\emotion C:\Users\joche\source\repos\SivarOs\Sivar.Os\Sivar.Os\wwwroot\models\emotion
```

**Time estimate:** 30-45 minutes total  
**Disk space needed:** 200 MB  
**Prerequisites:** Python 3.10+

---

## Overview

This guide explains how to quantize and bundle ONNX models with your Sivar.Os webapp for faster loading, offline support, and reduced bandwidth usage.

## Models Required

### 1. Sentiment Analysis Model
- **Model ID**: `lxyuan/distilbert-base-multilingual-cased-sentiments-student`
- **Purpose**: Multilingual sentiment classification (English/Spanish)
- **Output**: Positive, Neutral, Negative sentiment labels
- **Original Size**: ~250 MB
- **Quantized Size**: ~60-80 MB

### 2. Emotion Detection Model
- **Model ID**: `SamLowe/roberta-base-go_emotions`
- **Purpose**: Multi-label emotion classification (28 emotion categories)
- **Output**: Joy, Sadness, Anger, Fear, Surprise, Love, etc.
- **Original Size**: ~500 MB
- **Quantized Size**: ~125-150 MB

## Standalone Testing Project Setup

**RECOMMENDED:** Test quantization and models in a separate project before integrating into Sivar.Os.

### Create Test Project

```powershell
# Create a new directory for testing
mkdir C:\Temp\ModelQuantizationTest
cd C:\Temp\ModelQuantizationTest

# Create a simple HTML test file
New-Item -Path "test.html" -ItemType File
```

### test.html - Minimal Test Page

Create `test.html` with this content:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>ONNX Model Test</title>
</head>
<body>
    <h1>Sentiment Analysis Model Test</h1>
    <textarea id="textInput" rows="4" cols="50" placeholder="Enter text to analyze...">I love this community!</textarea>
    <br><br>
    <button onclick="analyzeText()">Analyze Sentiment</button>
    <br><br>
    <div id="results"></div>
    
    <script type="module">
        import { pipeline } from 'https://cdn.jsdelivr.net/npm/@xenova/transformers@2.6.0';
        
        let sentimentClassifier = null;
        let emotionClassifier = null;
        
        window.initModels = async function() {
            const results = document.getElementById('results');
            results.innerHTML = '<p>🔄 Loading models... This may take 30-60 seconds...</p>';
            
            try {
                // Test with LOCAL quantized models
                console.log('Loading sentiment model from ./models/sentiment/');
                sentimentClassifier = await pipeline(
                    'text-classification',
                    './models/sentiment/',
                    { quantized: true }
                );
                results.innerHTML += '<p>✅ Sentiment model loaded</p>';
                
                console.log('Loading emotion model from ./models/emotion/');
                emotionClassifier = await pipeline(
                    'text-classification',
                    './models/emotion/',
                    { topk: 5, quantized: true }
                );
                results.innerHTML += '<p>✅ Emotion model loaded</p>';
                results.innerHTML += '<p><strong>🎉 All models ready! Try analyzing text.</strong></p>';
            } catch (error) {
                results.innerHTML += `<p>❌ Error: ${error.message}</p>`;
                console.error('Model loading error:', error);
            }
        };
        
        window.analyzeText = async function() {
            const text = document.getElementById('textInput').value;
            const results = document.getElementById('results');
            
            if (!sentimentClassifier || !emotionClassifier) {
                results.innerHTML = '<p>⚠️ Models not loaded yet. Wait for initialization...</p>';
                return;
            }
            
            try {
                results.innerHTML = '<p>🔄 Analyzing...</p>';
                
                const sentimentResult = await sentimentClassifier(text);
                const emotionResult = await emotionClassifier(text);
                
                results.innerHTML = `
                    <h3>Sentiment Analysis Results:</h3>
                    <p><strong>Sentiment:</strong> ${sentimentResult[0].label} (${(sentimentResult[0].score * 100).toFixed(2)}%)</p>
                    <h3>Top Emotions:</h3>
                    <ul>
                        ${emotionResult.map(e => `<li>${e.label}: ${(e.score * 100).toFixed(2)}%</li>`).join('')}
                    </ul>
                `;
                
                console.log('Sentiment:', sentimentResult);
                console.log('Emotions:', emotionResult);
            } catch (error) {
                results.innerHTML = `<p>❌ Analysis error: ${error.message}</p>`;
                console.error('Analysis error:', error);
            }
        };
        
        // Auto-initialize on page load
        window.initModels();
    </script>
</body>
</html>
```

### Test Project Directory Structure

After quantization, your test project should look like:

```
C:\Temp\ModelQuantizationTest\
├── test.html
├── models\
│   ├── sentiment\
│   │   ├── model_quantized.onnx
│   │   ├── tokenizer.json
│   │   ├── tokenizer_config.json
│   │   ├── config.json
│   │   └── special_tokens_map.json
│   └── emotion\
│       ├── model_quantized.onnx
│       ├── tokenizer.json
│       ├── tokenizer_config.json
│       ├── config.json
│       └── special_tokens_map.json
```

### Run Local Web Server

You need a local web server to test (file:// protocol won't work with modules):

**Option 1: Python (Simplest)**
```powershell
# If Python is installed
python -m http.server 8000
# Then open: http://localhost:8000/test.html
```

**Option 2: Node.js**
```powershell
# Install http-server globally
npm install -g http-server

# Run server
http-server -p 8000
# Then open: http://localhost:8000/test.html
```

**Option 3: Visual Studio Code**
```powershell
# Install Live Server extension in VS Code
# Right-click test.html → "Open with Live Server"
```

### Testing Workflow

1. **Setup quantization environment** (see Prerequisites section below)
2. **Navigate to test directory:**
   ```powershell
   cd C:\Temp\ModelQuantizationTest
   mkdir models\sentiment
   mkdir models\emotion
   ```

3. **Quantize models into test directory:**
   ```powershell
   optimum-cli export onnx --model lxyuan/distilbert-base-multilingual-cased-sentiments-student --task text-classification --optimize O3 --quantize models\sentiment\
   
   optimum-cli export onnx --model SamLowe/roberta-base-go_emotions --task text-classification --optimize O3 --quantize models\emotion\
   ```

4. **Start web server and open test.html**

5. **Verify in browser console:**
   - Models load successfully
   - Analysis returns correct results
   - Check file sizes in DevTools Network tab

6. **If successful, copy models to Sivar.Os:**
   ```powershell
   # Copy tested models to your main project
   xcopy /E /I C:\Temp\ModelQuantizationTest\models\sentiment C:\Users\joche\source\repos\SivarOs\Sivar.Os\Sivar.Os\wwwroot\models\sentiment
   
   xcopy /E /I C:\Temp\ModelQuantizationTest\models\emotion C:\Users\joche\source\repos\SivarOs\Sivar.Os\Sivar.Os\wwwroot\models\emotion
   ```

---

---

## Environment Setup for Quantization

### Method 1: Python + Optimum (Recommended)

**Step 1: Install Python**
```powershell
# Check if Python is installed
python --version

# If not installed, download from:
# https://www.python.org/downloads/ (Python 3.10 or 3.11 recommended)
# ⚠️ CHECK "Add Python to PATH" during installation
```

**Step 2: Create Virtual Environment (Recommended)**
```powershell
cd C:\Temp\ModelQuantizationTest

# Create virtual environment
python -m venv venv

# Activate virtual environment
.\venv\Scripts\Activate.ps1

# If you get execution policy error:
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**Step 3: Install Required Packages**
```powershell
# Upgrade pip first
python -m pip install --upgrade pip

# Install Optimum with ONNX Runtime
pip install optimum[exporters,onnxruntime]

# Install additional dependencies
pip install transformers torch onnx

# Verify installation
optimum-cli --version
```

**Step 4: Test Installation**
```powershell
# This should show help text
optimum-cli export onnx --help
```

**Common Issues:**

❌ **"optimum-cli is not recognized"**
```powershell
# Add Python Scripts to PATH manually:
# For virtual env: C:\Temp\ModelQuantizationTest\venv\Scripts
# For system Python: C:\Users\<YourUser>\AppData\Local\Programs\Python\Python311\Scripts

# Or use full path:
python -m optimum.exporters.onnx --help
```

❌ **"No module named 'optimum'"**
```powershell
# Ensure virtual environment is activated
.\venv\Scripts\Activate.ps1
pip install optimum[exporters,onnxruntime]
```

---

### Method 2: Node.js + Transformers.js

**Step 1: Install Node.js**
```powershell
# Check if Node.js is installed
node --version
npm --version

# If not installed, download LTS version from:
# https://nodejs.org/ (v20.x recommended)
```

**Step 2: Install Transformers.js Converter**
```powershell
# Global installation
npm install -g @xenova/transformers

# Or use without installation (npx)
npx @xenova/transformers --help
```

**Step 3: Verify Installation**
```powershell
# Check version
npm list -g @xenova/transformers
```

---

### Environment Verification Checklist

Run these commands to verify your setup:

```powershell
# Check Python
python --version
# Expected: Python 3.10.x or 3.11.x

# Check pip
pip --version
# Expected: pip 24.x or newer

# Check Optimum (if using Method 1)
optimum-cli --version
# Expected: optimum 1.x.x

# OR Check Node.js (if using Method 2)
node --version
# Expected: v20.x.x

npm --version
# Expected: 10.x.x
```

---

## Prerequisites

Choose **ONE** of the following methods:

### Method 1: Using Optimum CLI (Recommended - Better Quantization)

**Requirements:**
- Python 3.8+ installed
- pip package manager

**Installation:**
```powershell
pip install optimum[exporters,onnxruntime]
```

### Method 2: Using Transformers.js CLI (Alternative)

**Requirements:**
- Node.js 18+ installed
- npm package manager

**Installation:**
```powershell
npm install -g @xenova/transformers
```

---

## Step-by-Step Instructions

### Step 1: Create Directory Structure

Navigate to your Sivar.Os project root and create model directories:

```powershell
cd c:\Users\joche\source\repos\SivarOs\Sivar.Os\Sivar.Os

# Create model directories
mkdir wwwroot\models\sentiment
mkdir wwwroot\models\emotion
```

### Step 2: Quantize Models

#### Option A: Using Optimum CLI (Recommended)

**Quantize Sentiment Model:**
```powershell
optimum-cli export onnx `
  --model lxyuan/distilbert-base-multilingual-cased-sentiments-student `
  --task text-classification `
  --optimize O3 `
  --quantize `
  wwwroot\models\sentiment\
```

**Quantize Emotion Model:**
```powershell
optimum-cli export onnx `
  --model SamLowe/roberta-base-go_emotions `
  --task text-classification `
  --optimize O3 `
  --quantize `
  wwwroot\models\emotion\
```

**Expected Output Files:**
```
wwwroot/models/sentiment/
  ├── model_quantized.onnx
  ├── tokenizer.json
  ├── tokenizer_config.json
  ├── config.json
  └── special_tokens_map.json

wwwroot/models/emotion/
  ├── model_quantized.onnx
  ├── tokenizer.json
  ├── tokenizer_config.json
  ├── config.json
  └── special_tokens_map.json
```

#### Option B: Using Transformers.js CLI

**Quantize Sentiment Model:**
```powershell
npx @xenova/transformers convert `
  --model_id lxyuan/distilbert-base-multilingual-cased-sentiments-student `
  --quantize `
  --output_dir wwwroot\models\sentiment\
```

**Quantize Emotion Model:**
```powershell
npx @xenova/transformers convert `
  --model_id SamLowe/roberta-base-go_emotions `
  --quantize `
  --output_dir wwwroot\models\emotion\
```

### Step 3: Verify Files

Check that all required files are present:

```powershell
# Check sentiment model
dir wwwroot\models\sentiment\

# Check emotion model
dir wwwroot\models\emotion\
```

**Required files for each model:**
- ✅ `model_quantized.onnx` (or `model.onnx`)
- ✅ `tokenizer.json`
- ✅ `tokenizer_config.json`
- ✅ `config.json`

### Step 4: Update Project Configuration

The sentiment-analyzer.js file has already been updated to use local models:

```javascript
// Models are loaded from /models/sentiment/ and /models/emotion/
this.sentimentClassifier = await pipeline(
    'text-classification',
    '/models/sentiment/',
    { quantized: true, local_files_only: true }
);

this.emotionClassifier = await pipeline(
    'text-classification',
    '/models/emotion/',
    { topk: 5, quantized: true, local_files_only: true }
);
```

### Step 5: Update .csproj (Optional but Recommended)

Add this to `Sivar.Os.csproj` to ensure models are copied to output directory:

```xml
<ItemGroup>
  <!-- Include ONNX models in build output -->
  <Content Include="wwwroot\models\**\*" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

### Step 6: Test the Models

1. **Build the project:**
```powershell
dotnet build
```

2. **Run the application:**
```powershell
dotnet run --project Sivar.Os\Sivar.Os.csproj
```

3. **Open browser DevTools** (F12) and navigate to the app

4. **Check console logs:**
```
[SentimentAnalyzer] Initializing models...
[SentimentAnalyzer] Sentiment model loaded
[SentimentAnalyzer] Emotion model loaded
[SentimentAnalyzer] ✅ All models ready
```

5. **Create a test post** with text: "I love this community!"

6. **Verify sentiment analysis** in console:
```
[SentimentAnalyzer] Analysis complete: positive (0.98)
[SentimentAnalyzer] Top emotions: joy (0.85), admiration (0.12)
```

---

---

## Complete Testing Workflow (Separate Project)

### Phase 1: Environment Setup (5-10 minutes)

- [ ] **Create test directory:** `C:\Temp\ModelQuantizationTest`
- [ ] **Install Python 3.10+** (if using Optimum method)
- [ ] **Create virtual environment:** `python -m venv venv`
- [ ] **Activate virtual environment:** `.\venv\Scripts\Activate.ps1`
- [ ] **Install Optimum:** `pip install optimum[exporters,onnxruntime]`
- [ ] **Verify installation:** `optimum-cli --version`

### Phase 2: Model Quantization (10-30 minutes)

- [ ] **Create model directories:**
  ```powershell
  mkdir models\sentiment
  mkdir models\emotion
  ```

- [ ] **Quantize sentiment model:**
  ```powershell
  optimum-cli export onnx `
    --model lxyuan/distilbert-base-multilingual-cased-sentiments-student `
    --task text-classification `
    --optimize O3 `
    --quantize `
    models\sentiment\
  ```
  ⏱️ Expected time: 5-10 minutes (downloads ~250 MB)

- [ ] **Quantize emotion model:**
  ```powershell
  optimum-cli export onnx `
    --model SamLowe/roberta-base-go_emotions `
    --task text-classification `
    --optimize O3 `
    --quantize `
    models\emotion\
  ```
  ⏱️ Expected time: 10-20 minutes (downloads ~500 MB)

- [ ] **Verify files exist:**
  ```powershell
  dir models\sentiment\*.onnx
  dir models\emotion\*.onnx
  ```

### Phase 3: Browser Testing (5 minutes)

- [ ] **Create test.html** (copy from "Standalone Testing Project Setup" section above)
- [ ] **Start web server:** `python -m http.server 8000`
- [ ] **Open browser:** `http://localhost:8000/test.html`
- [ ] **Open DevTools:** Press F12
- [ ] **Wait for models to load** (1-3 seconds from local files)
- [ ] **Check console for:** `✅ Sentiment model loaded` and `✅ Emotion model loaded`
- [ ] **Test with text:** "I love this community!"
- [ ] **Verify results:** Should show "positive" sentiment with "joy" emotion

### Phase 4: Integration (2 minutes)

✅ **If all tests pass, copy models to Sivar.Os:**

```powershell
# Navigate to Sivar.Os project
cd C:\Users\joche\source\repos\SivarOs\Sivar.Os\Sivar.Os

# Create directories if they don't exist
mkdir wwwroot\models\sentiment
mkdir wwwroot\models\emotion

# Copy tested models
xcopy /E /I C:\Temp\ModelQuantizationTest\models\sentiment wwwroot\models\sentiment
xcopy /E /I C:\Temp\ModelQuantizationTest\models\emotion wwwroot\models\emotion

# Verify copy
dir wwwroot\models\sentiment\*.onnx
dir wwwroot\models\emotion\*.onnx
```

- [ ] **Build Sivar.Os:** `dotnet build`
- [ ] **Run Sivar.Os:** `dotnet run`
- [ ] **Test sentiment analysis** in Sivar.Os
- [ ] **Verify database** shows sentiment fields populated

### Expected File Sizes

After quantization, you should see:

```
models\sentiment\
  model_quantized.onnx     ~60 MB  ✅
  tokenizer.json           ~2 MB
  config.json              ~1 KB
  
models\emotion\
  model_quantized.onnx     ~125 MB ✅
  tokenizer.json           ~2 MB
  config.json              ~1 KB
```

**Total disk space needed:** ~200 MB

---

## Troubleshooting

### Issue: "Model files not found"

**Solution:**
- Verify files exist in `wwwroot/models/sentiment/` and `wwwroot/models/emotion/`
- Check file names match exactly (case-sensitive on Linux/Docker)
- Ensure `config.json` exists in each directory

### Issue: "Quantization failed"

**Solution:**
- Update Python packages: `pip install --upgrade optimum onnxruntime`
- Try without quantization first: remove `--quantize` flag
- Check Python version: `python --version` (must be 3.8+)

### Issue: "Models still downloading from Hugging Face"

**Solution:**
- Clear browser cache (IndexedDB)
- Verify `local_files_only: true` is set in sentiment-analyzer.js
- Check browser console for path errors
- Ensure models are in `wwwroot/models/` not `wwwroot/js/models/`

### Issue: "Model inference errors"

**Solution:**
- Verify all tokenizer files are present
- Re-export models without optimization: remove `--optimize O3`
- Check browser console for ONNX runtime errors

---

## Advanced Configuration

### Custom Quantization Levels

Optimum supports different quantization levels:

```powershell
# Dynamic quantization (fastest, good accuracy)
optimum-cli export onnx --quantize --model MODEL_ID OUTPUT_DIR

# Static quantization (smaller, lower accuracy)
optimum-cli export onnx --quantize --calibration-dataset glue --model MODEL_ID OUTPUT_DIR
```

### Compression for Deployment

After quantization, compress models for production:

```powershell
# Install 7-Zip or use built-in compression
Compress-Archive -Path wwwroot\models\* -DestinationPath models.zip
```

Then configure your web server to serve `.onnx` files with gzip compression.

---

## File Size Comparison

| Model | Original | Quantized | Savings |
|-------|----------|-----------|---------|
| Sentiment (DistilBERT) | ~250 MB | ~60 MB | 76% |
| Emotion (RoBERTa) | ~500 MB | ~125 MB | 75% |
| **Total** | **~750 MB** | **~185 MB** | **75%** |

---

## Deployment Checklist

- [ ] Models quantized successfully
- [ ] All tokenizer files present in both directories
- [ ] Models copied to `wwwroot/models/sentiment/` and `wwwroot/models/emotion/`
- [ ] sentiment-analyzer.js updated to use local paths
- [ ] .csproj updated with `<Content Include>` (optional)
- [ ] Application builds without errors
- [ ] Browser console shows "All models ready"
- [ ] Test post creates successfully with sentiment data
- [ ] Database shows populated emotion fields

---

## Next Steps

1. **Test thoroughly** with various post content (EN/ES, positive/negative)
2. **Monitor performance** - quantized models should load in 1-3 seconds
3. **Consider CDN** - For production, host models on Azure Blob Storage or CDN
4. **Implement caching** - Models are already cached in browser IndexedDB
5. **Add fallback** - Keep server-side sentiment analysis for when client fails

---

## Resources

- [Optimum Documentation](https://huggingface.co/docs/optimum/index)
- [Transformers.js Documentation](https://huggingface.co/docs/transformers.js)
- [ONNX Runtime Optimization](https://onnxruntime.ai/docs/performance/quantization.html)
- [Model Cards on Hugging Face](https://huggingface.co/models)

---

## Support

If you encounter issues:
1. Check browser console for detailed error messages
2. Verify Python/Node.js versions
3. Try quantizing on a different machine
4. Test with unquantized models first
5. Check Hugging Face model pages for updates

---

**Last Updated:** October 31, 2025  
**Models Version:** Transformers.js v2.6.0  
**ONNX Runtime:** 1.16.0+
