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
    public int PrettifyMaxLength { get; set; } = 30000;
    public int PrettifyInstructionsMaxLength { get; set; } = 2000;
    public string AuditRetentionCron { get; set; } = "0 2 * * *";
  }
}
