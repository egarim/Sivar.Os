using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace MyTestApp.Client.Services
{
    // DelegatingHandler that intercepts 401 responses and triggers a top-level redirect
    public class UnauthorizedRedirectHandler : DelegatingHandler
    {
        private readonly IJSRuntime _jsRuntime;

        public UnauthorizedRedirectHandler(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Build return URL from current request path or default to '/'
                var returnUrl = "/";
                if (request.RequestUri != null)
                {
                    // Use the path component as a friendly return path
                    returnUrl = request.RequestUri.AbsolutePath;
                }

                var encoded = System.Uri.EscapeDataString(returnUrl);
                var loginUrl = $"/authentication/login?returnUrl={encoded}";

                // Call the named helper added to the host page to perform a top-level navigation
                try
                {
                    await _jsRuntime.InvokeVoidAsync("redirectTo", loginUrl);
                }
                catch
                {
                    // If JS interop fails, there's not much else we can do here
                }
            }

            return response;
        }
    }
}
