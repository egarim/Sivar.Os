
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.Services;
using System.Net.Http.Json;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Implementation of files client
/// </summary>
public class FilesClient : BaseClient, IFilesClient
{
    public FilesClient(HttpClient httpClient, SivarClientOptions options) 
        : base(httpClient, options) { }

    public async Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string contentType, string container = "posts", CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var response = await HttpClient.PostAsync($"api/fileupload/upload?container={container}", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<FileUploadResult>(cancellationToken);
        return result ?? throw new InvalidOperationException("Upload failed - no result returned");
    }

    public async Task<BulkFileUploadResult> UploadBulkAsync(IEnumerable<(Stream stream, string fileName, string contentType)> files, string container = "posts", CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        
        foreach (var (stream, fileName, contentType) in files)
        {
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "files", fileName);
        }

        var response = await HttpClient.PostAsync($"api/fileupload/upload-bulk?container={container}", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<BulkFileUploadResult>(cancellationToken);
        return result ?? throw new InvalidOperationException("Bulk upload failed - no result returned");
    }

    public async Task<string> GetFileUrlAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // API returns: { "fileId": "...", "url": "..." } in camelCase
        var result = await GetAsync<Dictionary<string, string>>($"api/files/{fileId}/url", cancellationToken);
        return result != null && result.TryGetValue("url", out var url) ? url : string.Empty;
    }

    public async Task<FileMetadata> GetFileMetadataAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<FileMetadata>($"api/files/{fileId}/metadata", cancellationToken);
    }

    public async Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"api/files/{fileId}", cancellationToken);
    }

    public async Task DeleteBulkAsync(IEnumerable<Guid> fileIds, CancellationToken cancellationToken = default)
    {
        await PostAsync($"api/files/delete-bulk", fileIds, cancellationToken);
    }
}
