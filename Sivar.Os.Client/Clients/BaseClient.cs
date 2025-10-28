using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Base class for all API clients with common HTTP operations
/// </summary>
public abstract class BaseClient
{
    protected readonly HttpClient HttpClient;
    protected readonly SivarClientOptions Options;
    protected readonly JsonSerializerOptions JsonOptions;

    protected BaseClient(HttpClient httpClient, SivarClientOptions options)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <summary>
    /// Sends GET request and deserializes response
    /// </summary>
    protected async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.GetAsync(endpoint, cancellationToken);
        return await HandleResponseAsync<T>(response);
    }

    /// <summary>
    /// Sends POST request with content and deserializes response
    /// </summary>
    protected async Task<TResponse> PostAsync<TResponse>(string endpoint, object? content = null, CancellationToken cancellationToken = default)
    {
        HttpContent? jsonContent = null;
        if (content != null)
        {
            var jsonString = JsonSerializer.Serialize(content, JsonOptions);
            jsonContent = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");
        }
        var response = await HttpClient.PostAsync(endpoint, jsonContent, cancellationToken);
        return await HandleResponseAsync<TResponse>(response);
    }

    /// <summary>
    /// Sends POST request without expecting response body
    /// </summary>
    protected async Task PostAsync(string endpoint, object? content = null, CancellationToken cancellationToken = default)
    {
        HttpContent? jsonContent = null;
        if (content != null)
        {
            var jsonString = JsonSerializer.Serialize(content, JsonOptions);
            jsonContent = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");
        }
        var response = await HttpClient.PostAsync(endpoint, jsonContent, cancellationToken);
        await EnsureSuccessAsync(response);
    }

    /// <summary>
    /// Sends PUT request with content and deserializes response
    /// </summary>
    protected async Task<TResponse> PutAsync<TResponse>(string endpoint, object content, CancellationToken cancellationToken = default)
    {
        var jsonString = JsonSerializer.Serialize(content, JsonOptions);
        var jsonContent = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");
        var response = await HttpClient.PutAsync(endpoint, jsonContent, cancellationToken);
        return await HandleResponseAsync<TResponse>(response);
    }

    /// <summary>
    /// Sends PUT request without expecting response body
    /// </summary>
    protected async Task PutAsync(string endpoint, object? content = null, CancellationToken cancellationToken = default)
    {
        HttpContent? jsonContent = null;
        if (content != null)
        {
            var jsonString = JsonSerializer.Serialize(content, JsonOptions);
            jsonContent = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");
        }
        var response = await HttpClient.PutAsync(endpoint, jsonContent, cancellationToken);
        await EnsureSuccessAsync(response);
    }

    /// <summary>
    /// Sends DELETE request without expecting response body
    /// </summary>
    protected async Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.DeleteAsync(endpoint, cancellationToken);
        await EnsureSuccessAsync(response);
    }

    /// <summary>
    /// Sends DELETE request and deserializes response
    /// </summary>
    protected async Task<T> DeleteAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.DeleteAsync(endpoint, cancellationToken);
        return await HandleResponseAsync<T>(response);
    }

    private async Task<T> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
                return default!;

            return JsonSerializer.Deserialize<T>(content, JsonOptions)!;
        }

        // For NOT FOUND (404) and UNAUTHORIZED (401), return null instead of throwing
        // This aligns with server-side behavior where these scenarios return null
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound || 
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return default!;
        }

        // Log detailed error information for debugging
        var errorContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[BaseClient] API Error: {response.StatusCode} {response.ReasonPhrase}");
        Console.WriteLine($"[BaseClient] Response Content: {errorContent}");
        
        await ThrowApiExceptionAsync(response);
        return default!;
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            // For NOT FOUND (404) and UNAUTHORIZED (401), return silently instead of throwing
            // This aligns with server-side behavior
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound || 
                response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return;
            }

            await ThrowApiExceptionAsync(response);
        }
    }

    private async Task ThrowApiExceptionAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        throw new SivarApiException(
            response.StatusCode,
            response.ReasonPhrase ?? "Unknown error",
            content
        );
    }
}
