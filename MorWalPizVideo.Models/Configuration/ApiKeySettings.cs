namespace MorWalPizVideo.Models.Configuration;

public class ApiKeySettings
{
    public string HeaderName { get; set; } = "X-API-Key";
    public int DefaultRateLimitPerMinute { get; set; } = 60;
    public bool EnableIpWhitelist { get; set; } = false;
}