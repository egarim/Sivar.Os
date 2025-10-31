# PostCard Image Display Fix

## Problem

After successfully uploading images to blob storage, the images were not displaying in the PostCard component. The image element appeared broken with "wetransfer.gif" shown in the attached screenshot.

## Root Cause

The `MapAttachmentsToDtosAsync` method in `PostService.cs` was using the raw `attachment.Url` value from the database instead of generating the proper public URL with proxy support.

**Original Code (Line 1089):**
```csharp
var attachmentDtos = attachments.Select(attachment => new PostAttachmentDto
{
    Id = attachment.Id,
    AttachmentType = attachment.AttachmentType,
    FileId = attachment.FileId,
    FilePath = attachment.Url,  // ❌ Raw database URL, no proxy
    OriginalFilename = attachment.OriginalFileName ?? "",
    MimeType = attachment.MimeType ?? "",
    FileSize = attachment.FileSizeBytes ?? 0,
    AltText = attachment.Description,
    DisplayOrder = attachment.DisplayOrder,
    CreatedAt = attachment.CreatedAt
}).ToList();
```

**Problem:**
- The `attachment.Url` field contains the blob storage path WITHOUT the proxy endpoint
- In development with Azurite, this causes CORS/Mixed Content errors
- The image src becomes `http://127.0.0.1:10000/...` instead of `/api/blob-proxy/...`

## Solution

Updated `MapAttachmentsToDtosAsync` to call `_fileStorageService.GetFileUrlAsync(attachment.FileId)` which internally uses `GeneratePublicUrl` to:
1. Detect if running against Azurite (localhost)
2. Return proxy URL for development: `/api/blob-proxy/{container}/{fileId}_{fileName}`
3. Return proper Azure URL for production

**Fixed Code:**
```csharp
var attachmentDtos = new List<PostAttachmentDto>();

foreach (var attachment in attachments)
{
    // ⭐ CRITICAL: Generate public URL with proxy support for development
    string publicUrl = attachment.Url; // Default to stored URL
    
    if (!string.IsNullOrEmpty(attachment.FileId))
    {
        try
        {
            // Use the file storage service to get the public URL with proxy support
            publicUrl = await _fileStorageService.GetFileUrlAsync(attachment.FileId);
            
            _logger.LogDebug("[PostService.MapAttachmentsToDtosAsync] Generated public URL - RequestId={RequestId}, FileId={FileId}, URL={URL}",
                requestId, attachment.FileId, publicUrl);
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning("[PostService.MapAttachmentsToDtosAsync] File not found in blob storage - RequestId={RequestId}, FileId={FileId}, falling back to stored URL",
                requestId, attachment.FileId);
            // Fall back to stored URL
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostService.MapAttachmentsToDtosAsync] Failed to generate public URL - RequestId={RequestId}, FileId={FileId}",
                requestId, attachment.FileId);
            // Fall back to original URL
        }
    }

    var dto = new PostAttachmentDto
    {
        Id = attachment.Id,
        AttachmentType = attachment.AttachmentType,
        FileId = attachment.FileId,
        FilePath = publicUrl, // ✅ Use generated public URL with proxy
        OriginalFilename = attachment.OriginalFileName ?? "",
        MimeType = attachment.MimeType ?? "",
        FileSize = attachment.FileSizeBytes ?? 0,
        AltText = attachment.Description,
        DisplayOrder = attachment.DisplayOrder,
        CreatedAt = attachment.CreatedAt
    };

    attachmentDtos.Add(dto);
}
```

## Changes Made

**File:** `PostService.cs`

1. **Changed mapping from LINQ to foreach loop** - Enables async calls within the loop
2. **Added `GetFileUrlAsync` call** - Fetches proper public URL with proxy detection
3. **Added error handling** - Falls back to stored URL if file lookup fails
4. **Added logging** - Debug logs for URL generation, warnings for file not found

## How It Works

### URL Generation Flow:

```
PostService.MapAttachmentsToDtosAsync()
         ↓
_fileStorageService.GetFileUrlAsync(fileId)
         ↓
AzureBlobStorageService.GetFileUrlAsync()
         ↓ (searches containers by metadata)
GeneratePublicUrl(blobUri, container, fileId, fileName)
         ↓
[Detects Azurite: localhost/127.0.0.1]
         ↓
Returns: /api/blob-proxy/posts/fileId_filename.gif
```

### Development vs Production:

**Development (Azurite):**
- Detects `127.0.0.1` or `localhost` in blob URI
- Returns proxy URL: `/api/blob-proxy/posts/{fileId}_{fileName}`
- Proxy serves via HTTPS, bypasses CORS

**Production (Azure Blob Storage):**
- Returns configured BaseUrl or direct Azure blob URL
- No proxy needed, direct access works

## Benefits

✅ **Images display correctly** - Proxy URLs work in development
✅ **No CORS errors** - Server-side proxy bypasses CORS issues
✅ **Production ready** - Automatically uses correct URL format per environment
✅ **Error resilient** - Falls back to stored URL if lookup fails
✅ **Comprehensive logging** - Tracks URL generation for debugging

## Testing

To verify the fix:
1. Upload an image/GIF with a post
2. Check server logs - should see: `Generated public URL - FileId=..., URL=/api/blob-proxy/posts/...`
3. View the post in the feed
4. Verify image displays correctly
5. Check browser DevTools Network tab - should see request to `/api/blob-proxy/posts/...`
6. No CORS errors in browser console

## Related Files

- `PostService.cs` - Updated `MapAttachmentsToDtosAsync` method
- `AzureBlobStorageService.cs` - `GetFileUrlAsync` and `GeneratePublicUrl` (existing)
- `Program.cs` - Blob proxy endpoint (existing)
- `PostCard.razor` - Displays images using `Post.Attachments[0].FilePath`

## Notes

- This fix applies to ALL post attachments (images, GIFs, etc.)
- The `GetFileUrlAsync` method searches all containers by metadata to find the file
- If a file is not found in blob storage, it falls back to the stored URL gracefully
- The proxy pattern is documented in `DEVELOPMENT_RULES.md` under "File Upload & Blob Storage"

---

**Date:** October 30, 2025  
**Issue:** Images uploaded to blob storage not displaying in PostCard  
**Status:** ✅ Fixed - PostService now generates proper proxy URLs
