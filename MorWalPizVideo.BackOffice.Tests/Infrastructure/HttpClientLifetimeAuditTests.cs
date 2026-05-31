using System.Text.RegularExpressions;

namespace MorWalPizVideo.BackOffice.Tests.Infrastructure;

/// <summary>
/// Source-scan audits enforcing HttpClient lifecycle invariants (FR-014, FR-015) in MorWalPizVideo.BackOffice.
/// </summary>
public class HttpClientLifetimeAuditTests
{
    private static string BackOfficeRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && dir.Name != "MorWalPizVideo.BackOffice.Tests" && dir.Parent is not null)
                dir = dir.Parent;
            var solutionRoot = dir?.Parent ?? throw new InvalidOperationException("Solution root not found.");
            return Path.Combine(solutionRoot.FullName, "MorWalPizVideo.BackOffice");
        }
    }

    private static IEnumerable<string> EnumerateBackOfficeSources()
    {
        var root = BackOfficeRoot;
        Assert.True(Directory.Exists(root), $"BackOffice project root not found at {root}");
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
}
