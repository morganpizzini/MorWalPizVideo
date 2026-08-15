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