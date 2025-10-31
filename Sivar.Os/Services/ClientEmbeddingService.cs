using Microsoft.JSInterop;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Client-side embedding service using Transformers.js for browser-based embedding generation
/// </summary>
public class ClientEmbeddingService : IClientEmbeddingService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ClientEmbeddingService> _logger;
    
    private IJSObjectReference? _module;
    private bool _isInitialized = false;
    private bool _isSupported = false;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public ClientEmbeddingService(
        IJSRuntime jsRuntime,
        ILogger<ClientEmbeddingService> logger)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initialize the JavaScript module (lazy loading, thread-safe)
    /// </summary>
    private async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized)
                return; // Double-check after acquiring lock

            _logger.LogInformation("[ClientEmbedding] Initializing JavaScript module...");

            try
            {
                _module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "./js/embeddings.js");

                _isSupported = await _module.InvokeAsync<bool>("isSupported");
                _isInitialized = true;

                if (_isSupported)
                {
                    _logger.LogInformation("[ClientEmbedding] Module initialized successfully - Client-side embeddings SUPPORTED");
                }
                else
                {
                    _logger.LogWarning("[ClientEmbedding] Module initialized but client-side embeddings NOT SUPPORTED (missing WebAssembly)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ClientEmbedding] Failed to initialize JavaScript module");
                _isSupported = false;
                _isInitialized = true; // Mark as initialized to prevent retry loops
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<float[]?> TryGenerateEmbeddingAsync(string text)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("[ClientEmbedding] Empty or null text provided");
            return null;
        }

        try
        {
            // Ensure module is initialized
            await InitializeAsync();

            // Check if client-side is supported
            if (!_isSupported || _module == null)
            {
                _logger.LogDebug("[ClientEmbedding] Client-side not supported, returning null for server fallback");
                return null;
            }

            _logger.LogInformation("[ClientEmbedding] Attempting client-side embedding generation for text ({Length} chars)...", 
                text.Length);

            var startTime = DateTime.UtcNow;

            // Call JavaScript function
            var result = await _module.InvokeAsync<float[]?>("generateEmbedding", text);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Validate result
            if (result == null)
            {
                _logger.LogWarning("[ClientEmbedding] JavaScript returned null, falling back to server-side");
                return null;
            }

            if (result.Length != 384)
            {
                _logger.LogError("[ClientEmbedding] Invalid embedding dimensions: {Dimensions} (expected 384)", 
                    result.Length);
                return null;
            }

            _logger.LogInformation("[ClientEmbedding] ✓ Successfully generated {Dimensions}D embedding in {Elapsed}ms", 
                result.Length, elapsed);

            return result;
        }
        catch (JSException jsEx)
        {
            _logger.LogError(jsEx, "[ClientEmbedding] JavaScript error during embedding generation");
            return null; // Fallback to server
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ClientEmbedding] Unexpected error during embedding generation");
            return null; // Fallback to server
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsSupportedAsync()
    {
        try
        {
            await InitializeAsync();
            return _isSupported;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ClientEmbedding] Error checking support");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> WarmupModelAsync()
    {
        try
        {
            await InitializeAsync();

            if (!_isSupported || _module == null)
            {
                _logger.LogDebug("[ClientEmbedding] Skipping warmup - client-side not supported");
                return false;
            }

            _logger.LogInformation("[ClientEmbedding] Starting model warmup (downloading ~25MB model)...");
            var startTime = DateTime.UtcNow;

            var success = await _module.InvokeAsync<bool>("warmupModel");

            var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;

            if (success)
            {
                _logger.LogInformation("[ClientEmbedding] ✓ Model warmup completed in {Elapsed:F1}s", elapsed);
            }
            else
            {
                _logger.LogWarning("[ClientEmbedding] Model warmup failed");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ClientEmbedding] Error during model warmup");
            return false;
        }
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ClientEmbedding] Error disposing module");
            }
        }

        _initLock.Dispose();
    }
}
