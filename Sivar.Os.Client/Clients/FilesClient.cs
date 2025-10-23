
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Implementation of files client
/// </summary>
public class FilesClient : BaseClient, IFilesClient
{
    public FilesClient(HttpClient httpClient, SivarClientOptions options) 
        : base(httpClient, options) { }

    // TODO: Implement file upload with multipart/form-data
    // Will require making HandleResponseAsync protected in BaseClient
    // public async Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)

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
}
