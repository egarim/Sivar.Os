namespace Sivar.Os.Client.Pages;

public class ProfileStats
{
    public int Posts { get; set; }
    public int Followers { get; set; }
    public int Following { get; set; }
}

public class ProfileData
{
    public string ProfileSlug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public ProfileStats Stats { get; set; } = new();
}
