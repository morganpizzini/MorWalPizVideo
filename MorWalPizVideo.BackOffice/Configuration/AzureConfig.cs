namespace MorWalPizVideo.BackOffice.Configuration
{
  public class AzureConfig
  {
    public OpenAi OpenAi { get; set; } = null!;
  }

  public class OpenAi
  {
    public string DeploymentName { get; set; } = null!;
    public string OpenAiEndpoint { get; set; } = null!;
    public string OpenAiKey { get; set; } = null!;
  }

  public class ScriptStudioOptions
  {
    public int GlobalPromptMaxLength { get; set; } = 4000;
    public string AuditRetentionCron { get; set; } = "0 2 * * *";
  }
}
