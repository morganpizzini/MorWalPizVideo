internal class PinterestSettings
{
    public string AppId { get; set; } = null!;
    public string AppSecret { get; set; } = null!;
}
internal class AzureConfig
{
    public OpenAi OpenAi { get; set; } = null!;
}
internal class OpenAi
{
    public string DeploymentName { get; set; } = null!;
    public string OpenAiEndpoint { get; set; } = null!;

    public string OpenAiKey { get; set; } = null!;
}