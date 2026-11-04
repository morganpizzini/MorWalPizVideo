using System.Text.RegularExpressions;

namespace MorWalPizVideo.BackOffice.Tests.Infrastructure;

/// <summary>
/// Source-scan audits enforcing HttpClient lifecycle invariants (FR-014, FR-015) in the API hosts.
/// </summary>
[Trait("Category", "TestGroup:Shared")]
public class HttpClientLifetimeAuditTests
{
    private static string SolutionRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && dir.Name != "MorWalPizVideo.BackOffice.Tests" && dir.Parent is not null)
                dir = dir.Parent;
            return dir?.Parent?.FullName ?? throw new InvalidOperationException("Solution root not found.");
        }
    }

    private static IEnumerable<string> EnumerateApiSources()
    {
        var roots = new[] { "MorWalPizVideo.BackOffice", "MorWalPizVideo.ServerAPI" }
            .Select(project => Path.Combine(SolutionRoot, project));
        foreach (var root in roots)
        {
            Assert.True(Directory.Exists(root), $"API project root not found at {root}");
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)))
            {
                yield return file;
            }
        }
    }

    private static IEnumerable<string> EnumerateBackOfficeSources()
        => EnumerateProjectSources(Path.Combine(SolutionRoot, "MorWalPizVideo.BackOffice"));

    private static IEnumerable<string> EnumerateProjectSources(string root)
    {
        Assert.True(Directory.Exists(root), $"API project root not found at {root}");
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void No_direct_HttpClient_construction_in_production_code()
    {
        var pattern = new Regex(@"new\s+HttpClient\s*\(", RegexOptions.Compiled);
        var offenders = new List<string>();

        foreach (var file in EnumerateBackOfficeSources())
        {
            var content = File.ReadAllText(file);
            if (pattern.IsMatch(content))
                offenders.Add(Path.GetFileName(file));
        }

        Assert.True(offenders.Count == 0,
            "Found direct `new HttpClient(...)` constructions (FR-014 violation): " + string.Join(", ", offenders));
    }

    [Fact]
    public void No_using_var_on_CreateClient_in_production_code()
    {
        var pattern = new Regex(@"using\s+var\s+\w+\s*=\s*[^;]*\.CreateClient\(", RegexOptions.Compiled);
        var offenders = new List<string>();

        foreach (var file in EnumerateBackOfficeSources())
        {
            var content = File.ReadAllText(file);
            if (pattern.IsMatch(content))
                offenders.Add(Path.GetFileName(file));
        }

        Assert.True(offenders.Count == 0,
            "Found `using var ... = factory.CreateClient(...)` patterns that dispose IHttpClientFactory-managed clients (FR-014 violation): " + string.Join(", ", offenders));
    }

    [Fact]
    public void No_direct_WebPushClient_construction_in_api_production_code()
    {
        var pattern = new Regex(@"new\s+WebPushClient\s*\(\s*\)", RegexOptions.Compiled);
        var offenders = new List<string>();

        foreach (var file in EnumerateApiSources())
        {
            var content = File.ReadAllText(file);
            if (pattern.IsMatch(content))
                offenders.Add(Path.GetFileName(file));
        }

        Assert.True(offenders.Count == 0,
            "Found direct `new WebPushClient()` constructions (FR-014 violation): " + string.Join(", ", offenders));
    }
}
