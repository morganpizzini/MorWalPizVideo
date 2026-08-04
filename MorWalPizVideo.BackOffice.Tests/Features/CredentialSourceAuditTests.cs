using System.Text.RegularExpressions;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed partial class CredentialSourceAuditTests
{
  private const string Placeholder = "REPLACE_WITH_API_KEY";

  [Fact]
  public void Desktop_api_key_seed_artifacts_contain_placeholders_only()
  {
    var repositoryRoot = FindRepositoryRoot();
    var relativePaths = new[]
    {
            "MorWalPiz.VideoImporter/Data/AppDbContext.cs",
            "MorWalPiz.VideoImporter/Migrations/20260410093957_apiKeyData.cs",
            "MorWalPiz.VideoImporter/Migrations/20260410093957_apiKeyData.Designer.cs",
            "MorWalPiz.VideoImporter/Migrations/20260424145854_updateModel.Designer.cs",
            "MorWalPiz.VideoImporter/Migrations/AppDbContextModelSnapshot.cs"
        };
    var violations = new List<string>();

    foreach (var relativePath in relativePaths)
    {
      var lines = File.ReadAllLines(Path.Combine(repositoryRoot, relativePath));
      for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
      {
        var match = ApiKeyAssignmentRegex().Match(lines[lineIndex]);
        if (!match.Success && relativePath.EndsWith("apiKeyData.cs", StringComparison.Ordinal))
        {
          match = MigrationValueRegex().Match(lines[lineIndex]);
        }

        if (match.Success && !string.Equals(match.Groups["value"].Value, Placeholder, StringComparison.Ordinal))
        {
          violations.Add($"{relativePath}:{lineIndex + 1}");
        }
      }
    }

    Assert.Empty(violations);
  }

  private static string FindRepositoryRoot()
  {
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MorWalPizVideo.sln")))
    {
      directory = directory.Parent;
    }

    return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
  }

  [GeneratedRegex("ApiKey\\s*=\\s*\"(?<value>[^\"]+)\"")]
  private static partial Regex ApiKeyAssignmentRegex();

  [GeneratedRegex("value:\\s*\"(?<value>[^\"]+)\"")]
  private static partial Regex MigrationValueRegex();
}