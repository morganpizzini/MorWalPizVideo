namespace MorWalPizVideo.BackOffice.Configuration;

public static class HangfireConfiguration
{
  public static string GetRequiredConnectionString(IConfiguration configuration)
  {
    var connectionString = configuration.GetConnectionString("HangfireConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
      throw new InvalidOperationException(
          "FeatureManagement:EnableHangFire requires durable ConnectionStrings:HangfireConnection storage.");
    }

    return connectionString;
  }
}