using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Sivar.Os.Services;

/// <summary>
/// Service to configure CORS for Azure Blob Storage (Azurite in development)
/// </summary>
public class BlobStorageCorsConfigurator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BlobStorageCorsConfigurator> _logger;

    public BlobStorageCorsConfigurator(
        IConfiguration configuration,
        ILogger<BlobStorageCorsConfigurator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Configure CORS rules for Azurite/Azure Blob Storage
    /// </summary>
    public async Task ConfigureCorsAsync()
    {
        try
        {
            var connectionString = _configuration["AzureBlobStorage:ConnectionString"];
            
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("[BlobStorageCorsConfigurator] No connection string found, skipping CORS configuration");
                return;
            }

            var serviceClient = new BlobServiceClient(connectionString);

            _logger.LogInformation("[BlobStorageCorsConfigurator] Configuring CORS for Blob Storage...");

            // Get current properties
            var properties = await serviceClient.GetPropertiesAsync();

            // Configure CORS rules
            var corsRules = new List<BlobCorsRule>
            {
                new BlobCorsRule
                {
                    AllowedOrigins = "*", // Allow all origins in development
                    AllowedMethods = "GET,POST,PUT,DELETE,HEAD,OPTIONS",
                    AllowedHeaders = "*",
                    ExposedHeaders = "*",
                    MaxAgeInSeconds = 3600
                }
            };

            properties.Value.Cors = corsRules;

            // Apply CORS settings
            await serviceClient.SetPropertiesAsync(properties.Value);

            _logger.LogInformation("[BlobStorageCorsConfigurator] CORS configured successfully - AllowedOrigins: *, AllowedMethods: GET,POST,PUT,DELETE,HEAD,OPTIONS");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BlobStorageCorsConfigurator] Failed to configure CORS - this may cause image loading issues");
            // Don't throw - CORS configuration failure shouldn't prevent app startup
        }
    }
}
