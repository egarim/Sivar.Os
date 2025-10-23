using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.JSInterop;

namespace Sivar.Os.Client.Auth
{
    /// <summary>
    /// WASM Authentication State Provider for Blazor Hybrid Auto mode
    /// Fetches auth state from the server's /authentication/profile endpoint
    /// </summary>
    public class WasmAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private AuthenticationState? _cachedState;
        private static int _callCount = 0;

        public WasmAuthenticationStateProvider(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var callNumber = ++_callCount;
            Console.WriteLine($"[WasmAuthStateProvider] GetAuthenticationStateAsync called (call #{callNumber})");
            
            try
            {
                // Use JS fetch with credentials include so cookies are sent from the browser
                var jsonText = await _jsRuntime.InvokeAsync<string>("fetchWithCredentials", "authentication/profile");
                Console.WriteLine($"[WasmAuthStateProvider] Call #{callNumber}: Fetch returned {(string.IsNullOrWhiteSpace(jsonText) ? "empty" : "profile data")}");
                
                if (string.IsNullOrWhiteSpace(jsonText))
                {
                    var unauthState = new AuthenticationState(new ClaimsPrincipal());
                    Console.WriteLine($"[WasmAuthStateProvider] Call #{callNumber}: Returning unauthenticated state (empty response)");
                    _cachedState = unauthState;
                    return unauthState;
                }

                // Log the raw response (first 200 chars)
                var responsePreview = jsonText.Length > 200 ? jsonText.Substring(0, 200) + "..." : jsonText;
                Console.WriteLine($"[WasmAuthStateProvider] Call #{callNumber}: Raw profile response: {responsePreview}");

                var json = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonText);
                Console.WriteLine($"[WasmAuthStateProvider] Call #{callNumber}: Deserialized json keys: {(json != null ? string.Join(", ", json.Keys) : "null")}");
                
                bool isAuth = false;
                if (json != null && json.TryGetValue("isAuthenticated", out var isAuthObj))
                {
                    Console.WriteLine($"[WasmAuthStateProvider] Call #{callNumber}: isAuthenticated value: {isAuthObj} (type: {isAuthObj?.GetType().Name})");
                    
                    // Handle both bool and JsonElement representations
                    if (isAuthObj is bool b)
                    {
                        isAuth = b;
                    }
                    else if (isAuthObj is System.Text.Json.JsonElement elem)
                    {
                        isAuth = elem.ValueKind == System.Text.Json.JsonValueKind.True;
                    }
                    
                    Console.WriteLine($"[WasmAuthStateProvider] Call #{callNumber}: Parsed isAuth={isAuth}");
                }
                
                if (json != null && isAuth)
                {
                    var parsedClaims = ParseClaimsFromDictionary(json).ToList();
                    Console.WriteLine($"[WasmAuthStateProvider] Call #{callNumber}: Parsed {parsedClaims.Count} claims");
                    foreach (var claim in parsedClaims)
                    {
                        Console.WriteLine($"[WasmAuthStateProvider] Call #{callNumber}: Claim: {claim.Type}={claim.Value}");
                    }
                    
                    // Ensure we have a "name" claim with the standard ClaimTypes.Name type for compatibility
                    var nameClaim = parsedClaims.FirstOrDefault(c => c.Type == "name");
                    if (nameClaim != null && !parsedClaims.Any(c => c.Type == ClaimTypes.Name))
                    {
                        // Add ClaimTypes.Name if only "name" exists
                        parsedClaims.Add(new Claim(ClaimTypes.Name, nameClaim.Value));
                        Console.WriteLine($"[WasmAuthStateProvider] Call #{callNumber}: Added ClaimTypes.Name claim: {nameClaim.Value}");
                    }
                    
                    var identity = new ClaimsIdentity(
                        claims: parsedClaims,
                        authenticationType: "Server"
                    );

                    var user = new ClaimsPrincipal(identity);
                    var authState = new AuthenticationState(user);
                    
                    Console.WriteLine($"[WasmAuthStateProvider] Call #{callNumber}: Profile indicates authenticated, checking if state changed...");
                    
                    // Check if state changed; if so, notify subscribers (e.g., AuthorizeView)
                    if (_cachedState == null || !IsStateEqual(_cachedState, authState))
                    {
                        Console.WriteLine($"[WasmAuthStateProvider] Call #{callNumber}: State CHANGED! Notifying subscribers...");
                        _cachedState = authState;
                        NotifyAuthenticationStateChanged(Task.FromResult(authState));
                    }
                    else
                    {
                        Console.WriteLine($"[WasmAuthStateProvider] Call #{callNumber}: State unchanged, not notifying");
                    }
                    
                    return authState;
                }

                var unauthState2 = new AuthenticationState(new ClaimsPrincipal());
                Console.WriteLine($"[WasmAuthStateProvider] Call #{callNumber}: Profile indicates NOT authenticated, checking if state changed...");
                if (_cachedState == null || !IsStateEqual(_cachedState, unauthState2))
                {
                    Console.WriteLine($"[WasmAuthStateProvider] Call #{callNumber}: State CHANGED to unauthenticated! Notifying subscribers...");
                    _cachedState = unauthState2;
                    NotifyAuthenticationStateChanged(Task.FromResult(unauthState2));
                }
                else
                {
                    Console.WriteLine($"[WasmAuthStateProvider] Call #{callNumber}: Unauthenticated state unchanged, not notifying");
                }
                return unauthState2;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WasmAuthStateProvider] Call #{callNumber}: Exception: {ex.Message}");
                var unauthState = new AuthenticationState(new ClaimsPrincipal());
                if (_cachedState == null || !IsStateEqual(_cachedState, unauthState))
                {
                    _cachedState = unauthState;
                    NotifyAuthenticationStateChanged(Task.FromResult(unauthState));
                }
                return unauthState;
            }
        }

        private IEnumerable<Claim> ParseClaimsFromDictionary(Dictionary<string, object> json)
        {
            var claims = new List<Claim>();
            
            // First, extract claims array (it will be a JsonElement)
            if (json.TryGetValue("claims", out var claimsObj))
            {
                List<object>? claimsList = null;
                
                if (claimsObj is List<object> list)
                {
                    claimsList = list;
                }
                else if (claimsObj is System.Text.Json.JsonElement claimsElem && claimsElem.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    // Deserialize JsonElement array to List<object>
                    claimsList = System.Text.Json.JsonSerializer.Deserialize<List<object>>(claimsElem.GetRawText());
                }
                
                if (claimsList != null)
                {
                    Console.WriteLine($"[WasmAuthStateProvider] Found {claimsList.Count} claims in profile");
                    foreach (var claimObj in claimsList)
                    {
                        Dictionary<string, object>? claimDict = null;
                        
                        if (claimObj is Dictionary<string, object> dict)
                        {
                            claimDict = dict;
                        }
                        else if (claimObj is System.Text.Json.JsonElement claimElem)
                        {
                            claimDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(claimElem.GetRawText());
                        }
                        
                        if (claimDict != null &&
                            claimDict.TryGetValue("type", out var type) && 
                            claimDict.TryGetValue("value", out var value))
                        {
                            string? typeStr = ExtractStringValue(type);
                            string? valueStr = ExtractStringValue(value);
                            
                            if (!string.IsNullOrEmpty(typeStr) && !string.IsNullOrEmpty(valueStr))
                            {
                                claims.Add(new Claim(typeStr, valueStr));
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"[WasmAuthStateProvider] Claims value type: {claimsObj?.GetType().Name}");
                }
            }
            else
            {
                Console.WriteLine($"[WasmAuthStateProvider] No claims key found in profile");
            }

            return claims;
        }
        
        private string? ExtractStringValue(object obj)
        {
            if (obj is string str)
                return str;
            
            if (obj is System.Text.Json.JsonElement elem)
            {
                if (elem.ValueKind == System.Text.Json.JsonValueKind.String)
                    return elem.GetString();
            }
            
            return null;
        }

        private bool IsStateEqual(AuthenticationState state1, AuthenticationState state2)
        {
            var user1 = state1.User;
            var user2 = state2.User;
            var isAuth1 = user1.Identity?.IsAuthenticated ?? false;
            var isAuth2 = user2.Identity?.IsAuthenticated ?? false;
            return isAuth1 == isAuth2;
        }
    }
}
