internal class TelegramSettings
{
    public string Token { get; set; } = null!;
    public string ChannelName { get; set; } = null!;
}

internal class TranslatorSettings
{
    public string SubscriptionKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}
