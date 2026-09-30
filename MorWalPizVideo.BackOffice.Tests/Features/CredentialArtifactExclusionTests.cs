using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class CredentialArtifactExclusionTests
{
    [Theory]
    [InlineData("MorWalPizVideo.ServerAPI")]
    [InlineData("MorWalPizVideo.BackOffice")]
    public async Task Evaluated_credential_sentinel_is_never_copied_to_output_or_publish(string project)
    {
        var root = RepositoryRoot();
        var sentinelDirectory = Path.Combine(root, project, "security-sentinel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sentinelDirectory);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(sentinelDirectory, "credentials-sentinel.json"), "{\"securityAuditSentinel\":true}");
            var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            foreach (var argument in new[] { "msbuild", Path.Combine(project, project + ".csproj"), "-nologo", "-getItem:Content,None" }) start.ArgumentList.Add(argument);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("MSBuild audit did not start.");
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            try { await process.WaitForExitAsync(deadline.Token); }
            catch (OperationCanceledException) { process.Kill(true); throw; }
            process.ExitCode.Should().Be(0, "MSBuild metadata evaluation must succeed");
            (await errors).Should().BeEmpty();
            using var result = JsonDocument.Parse(await output);
            var sentinel = result.RootElement.GetProperty("Items").EnumerateObject().SelectMany(item => item.Value.EnumerateArray())
                .Single(item => item.GetProperty("Identity").GetString()!.Contains(Path.GetFileName(sentinelDirectory), StringComparison.Ordinal));
            sentinel.GetProperty("CopyToOutputDirectory").GetString().Should().Be("Never");
            sentinel.GetProperty("CopyToPublishDirectory").GetString().Should().Be("Never");
        }
        finally { Directory.Delete(sentinelDirectory, true); }
    }

    [Theory]
    [InlineData("MorWalPizVideo.ServerAPI")]
    [InlineData("MorWalPizVideo.BackOffice")]
    public void Docker_restore_inputs_include_every_transitive_project_manifest(string project)
    {
        var root = RepositoryRoot();
        var beforeRestore = File.ReadAllText(Path.Combine(root, project, "Dockerfile")).Split("RUN dotnet restore", 2)[0];
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Verify(string manifest)
        {
            if (!visited.Add(manifest)) return;
            beforeRestore.Should().Contain("COPY [\"" + Path.GetRelativePath(root, manifest).Replace('\\', '/') + "\"");
            foreach (var reference in XDocument.Load(manifest).Descendants("ProjectReference"))
                Verify(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(manifest)!, reference.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar))));
        }
        Verify(Path.Combine(root, project, project + ".csproj"));
        File.ReadAllText(Path.Combine(root, ".dockerignore")).Should().Contain("**/credentials*.json");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MorWalPizVideo.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}