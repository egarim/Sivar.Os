using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Sivar.Os.Controllers;

[Route("[controller]/[action]")]
public class CultureController : Controller
{
    /// <summary>
    /// Sets the culture cookie and redirects back to the referring page
    /// </summary>
    /// <param name="culture">Culture code (e.g., "en-US", "es-ES")</param>
    /// <param name="redirectUri">URI to redirect to after setting culture</param>
    [HttpGet]
    public IActionResult SetCulture(string culture, string redirectUri)
    {
        if (!string.IsNullOrEmpty(culture))
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions 
                { 
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    Path = "/",
                    SameSite = SameSiteMode.Lax
                }
            );
        }

        return LocalRedirect(redirectUri ?? "/");
    }
}
