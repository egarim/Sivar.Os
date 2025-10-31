# Azurite CORS Configuration Fix

## Problem
Your Blazor app runs on **HTTPS** (`https://localhost:5001`) but Azurite (Azure Storage Emulator) runs on **HTTP** (`http://127.0.0.1:10000`).

This causes:
1. ❌ **Mixed Content** errors - Browser blocks HTTP requests from HTTPS pages
2. ❌ **CORS** errors - Azurite doesn't allow cross-origin requests by default
3. ❌ **Images won't display** in posts

## Solution: Enable CORS in Azurite

### Option 1: Start Azurite with CORS Enabled (Recommended)

Stop your current Azurite instance and restart it with CORS configuration:

```powershell
# Stop Azurite if running
Stop-Process -Name "azurite" -Force -ErrorAction SilentlyContinue

# Start Azurite with CORS enabled for all origins
azurite --blobCors "*"
```

Or with specific origin:

```powershell
azurite --blobCors "https://localhost:5001,http://localhost:5000"
```

### Option 2: Use docker-compose with CORS

If you're using Docker:

```yaml
version: '3.8'
services:
  azurite:
    image: mcr.microsoft.com/azure-storage/azurite
    ports:
      - "10000:10000"
      - "10001:10001"
      - "10002:10002"
    command: "azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --blobCors '*'"
```

### Option 3: Programmatically Set CORS (In Code)

Add this initialization code to set CORS rules:

```csharp
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

public async Task ConfigureAzuriteCorsAsync()
{
    var serviceClient = new BlobServiceClient("UseDevelopmentStorage=true");
    
    var corsRules = new List<BlobCorsRule>
    {
        new BlobCorsRule
        {
            AllowedOrigins = "https://localhost:5001,http://localhost:5000",
            AllowedMethods = "GET,POST,PUT,DELETE,HEAD,OPTIONS",
            AllowedHeaders = "*",
            ExposedHeaders = "*",
            MaxAgeInSeconds = 3600
        }
    };
    
    var properties = await serviceClient.GetPropertiesAsync();
    properties.Value.Cors = corsRules;
    await serviceClient.SetPropertiesAsync(properties);
}
```

## Verify CORS is Working

1. **Restart Azurite** with CORS enabled
2. **Refresh your browser** (hard refresh: Ctrl+Shift+R)
3. **Upload an image** to a post
4. **Check browser console** - CORS errors should be gone

## Expected Console Output

✅ **Before Fix:**
```
Access to image at 'http://127.0.0.1:10000/...' has been blocked by CORS policy
```

✅ **After Fix:**
```
(No CORS errors - images load successfully)
```

## Alternative: Use HTTPS Proxy for Azurite

If CORS still doesn't work, you can proxy Azurite through your ASP.NET Core app:

### Add to Program.cs:

```csharp
app.MapGet("/api/files/{container}/{*blobPath}", async (
    string container, 
    string blobPath,
    IFileStorageService fileStorage) =>
{
    var fileUrl = await fileStorage.GetFileUrlAsync(blobPath);
    // Proxy the request to Azurite
    using var httpClient = new HttpClient();
    var response = await httpClient.GetAsync(fileUrl);
    var stream = await response.Content.ReadAsStreamAsync();
    var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
    
    return Results.Stream(stream, contentType);
});
```

Then update `GeneratePublicUrl` to use the proxy:

```csharp
private string GeneratePublicUrl(Uri blobUri, string container, string fileId, string fileName)
{
    // Use server proxy endpoint instead of direct Azurite URL
    return $"/api/files/{container}/{fileId}_{fileName}";
}
```

## Quick Test Command

```powershell
# Test if Azurite is accepting CORS requests
curl -X OPTIONS http://127.0.0.1:10000/devstoreaccount1/sivaros-posts -H "Origin: https://localhost:5001" -H "Access-Control-Request-Method: GET" -v
```

Look for `Access-Control-Allow-Origin: *` in the response headers.
