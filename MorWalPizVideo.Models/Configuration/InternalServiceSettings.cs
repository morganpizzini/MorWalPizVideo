namespace MorWalPizVideo.Models.Configuration;

// Shared-secret settings for authenticating trusted service-to-service calls (e.g. BackOffice -> ServerAPI cache endpoints).
public class InternalServiceSettings
{
    public string HeaderName { get; set; } = "X-Internal-Service-Key";
    public string Secret { get; set; } = string.Empty;
}
