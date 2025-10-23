using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace MyTestApp.Client.Services
{
    /// <summary>
    /// Simple API client wrapper to centralize 401 handling and JS redirect behavior.
    /// </summary>
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;

        public ApiClient(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        public async Task<T?> GetJsonAsync<T>(string requestUri)
        {
            var response = await _httpClient.GetAsync(requestUri);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Build a friendly returnUrl (use the request path)
                var returnUrl = requestUri;
                if (requestUri.StartsWith("/"))
                {
                    returnUrl = requestUri;
                }

                var encoded = System.Uri.EscapeDataString(returnUrl);
                var loginUrl = $"/authentication/login?returnUrl={encoded}";

                try
                {
                    await _jsRuntime.InvokeVoidAsync("redirectTo", loginUrl);
                }
                catch
                {
                    // swallow JS exceptions - worst case the app remains on current page
                }

                return default;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T?>();
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            var response = await _httpClient.GetAsync(requestUri);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var encoded = System.Uri.EscapeDataString(requestUri);
                var loginUrl = $"/authentication/login?returnUrl={encoded}";
                try
                {
                    await _jsRuntime.InvokeVoidAsync("redirectTo", loginUrl);
                }
                catch
                {
                }
            }

            return response;
        }
    }
}
