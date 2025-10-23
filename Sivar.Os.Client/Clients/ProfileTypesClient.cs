
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Implementation of profile types client
/// </summary>
public class ProfileTypesClient : BaseClient, IProfileTypesClient
{
    public ProfileTypesClient(HttpClient httpClient, SivarClientOptions options) 
        : base(httpClient, options) { }

    public async Task<IEnumerable<ProfileTypeDto>> GetActiveProfileTypesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ProfileTypeDto>>("api/profiletypes", cancellationToken);
    }

    public async Task<IEnumerable<ProfileTypeDto>> GetAllProfileTypesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ProfileTypeDto>>("api/profiletypes/all", cancellationToken);
    }

    public async Task<ProfileTypeDto> GetProfileTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<ProfileTypeDto>($"api/profiletypes/{id}", cancellationToken);
    }

    public async Task<IEnumerable<ProfileTypeDto>> GetProfileTypesWithUsageAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ProfileTypeDto>>("api/profiletypes/with-usage", cancellationToken);
    }

    public async Task<ProfileTypeDto> CreateProfileTypeAsync(CreateProfileTypeDto request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<ProfileTypeDto>("api/profiletypes", request, cancellationToken);
    }

    public async Task<ProfileTypeDto> UpdateProfileTypeAsync(Guid id, UpdateProfileTypeDto request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<ProfileTypeDto>($"api/profiletypes/{id}", request, cancellationToken);
    }

    public async Task DeleteProfileTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"api/profiletypes/{id}", cancellationToken);
    }

    public async Task<ProfileTypeDto> ActivateProfileTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await PutAsync<ProfileTypeDto>($"api/profiletypes/{id}/activate", new { }, cancellationToken);
    }

    public async Task<ProfileTypeDto> DeactivateProfileTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await PutAsync<ProfileTypeDto>($"api/profiletypes/{id}/deactivate", new { }, cancellationToken);
    }

    public async Task UpdateSortOrdersAsync(Dictionary<Guid, int> sortOrders, CancellationToken cancellationToken = default)
    {
        await PutAsync<object>("api/profiletypes/sort-orders", sortOrders, cancellationToken);
    }

    public async Task<bool> CheckNameAvailabilityAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = $"api/profiletypes/check-name-availability?name={Uri.EscapeDataString(name)}";
        if (excludeId.HasValue)
            query += $"&excludeId={excludeId.Value}";

        var result = await GetAsync<dynamic>(query, cancellationToken);
        return result?.isAvailable ?? false;
    }
}
