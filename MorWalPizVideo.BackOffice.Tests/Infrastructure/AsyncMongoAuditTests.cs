using System.Text.RegularExpressions;

namespace MorWalPizVideo.BackOffice.Tests.Infrastructure;

/// <summary>
/// Source-scan audit ensuring controllers don't mix sync Mongo calls with async methods (FR-016).
/// </summary>
public class AsyncMongoAuditTests
{
    private static string ControllersRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && dir.Name != "MorWalPizVideo.BackOffice.Tests" && dir.Parent is not null)
                dir = dir.Parent;
            var solutionRoot = dir?.Parent ?? throw new InvalidOperationException("Solution root not found.");
            return Path.Combine(solutionRoot.FullName, "MorWalPizVideo.BackOffice", "Controllers");
        }
    }

    private static readonly Regex SyncMongoCall = new(
        @"\.(?:Find|InsertOne|InsertMany|ReplaceOne|UpdateOne|UpdateMany|DeleteOne|DeleteMany|CountDocuments|Aggregate)\b(?!Async)",
        RegexOptions.Compiled);

    private static readonly Regex CursorSync = new(
        @"\.(?:FirstOrDefault|First|Single|SingleOrDefault|ToList|ToCursor|ToEnumerable|Any|Count)\(\)",
        RegexOptions.Compiled);

    [Fact]
    public void Controllers_do_not_use_sync_cursor_terminators()
    {
        var offenders = new List<string>();

        Assert.True(Directory.Exists(ControllersRoot), $"Controllers root not found at {ControllersRoot}");

        foreach (var file in Directory.EnumerateFiles(ControllersRoot, "*.cs", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Quick filter: only flag lines that look like they call into a Mongo collection / cursor.
                if (!line.Contains("Find(") && !line.Contains("Collection<") && !line.Contains("collection.") && !line.Contains("Builders<"))
                    continue;

                if (CursorSync.IsMatch(line))
                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}: {line.Trim()}");
            }
        }

        Assert.True(offenders.Count == 0,
            "Found sync cursor terminators on Mongo cursors inside controllers (FR-016 violation):\n" + string.Join("\n", offenders));
    }
}
