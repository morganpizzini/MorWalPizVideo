namespace MorWalPizVideo.YouTubeUtilities;

public sealed class YouTubeIdempotencyGuard
{
    private readonly HashSet<string> _completedKeys = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public bool TryBegin(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        lock (_lock)
            return _completedKeys.Add(key);
    }

    public void Cancel(string key)
    {
        lock (_lock)
            _completedKeys.Remove(key);
    }
}

public enum YouTubeIntentState
{
    InProgress,
    Completed,
    Unknown
}

public sealed record YouTubeIntent(string Key, YouTubeIntentState State, string? YouTubeId = null);

public sealed class DurableYouTubeIntentStore
{
    private readonly string _directory;

    public DurableYouTubeIntentStore(string directory)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        Directory.CreateDirectory(_directory);
    }

    public bool TryBegin(string key, out YouTubeIntent intent)
    {
        var path = GetPath(key);
        try
        {
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            Write(stream, new YouTubeIntent(key, YouTubeIntentState.InProgress));
            intent = new YouTubeIntent(key, YouTubeIntentState.InProgress);
            return true;
        }
        catch (IOException) when (File.Exists(path))
        {
            intent = Read(path, key);
            return false;
        }
    }

    public void Complete(string key, string youtubeId) => WriteFile(key, new YouTubeIntent(key, YouTubeIntentState.Completed, youtubeId));

    public void MarkUnknown(string key) => WriteFile(key, new YouTubeIntent(key, YouTubeIntentState.Unknown));

    public void Cancel(string key)
    {
        var path = GetPath(key);
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            MarkUnknown(key);
        }
    }

    private string GetPath(string key) => Path.Combine(_directory, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key))) + ".json");

    private void WriteFile(string key, YouTubeIntent intent)
    {
        var path = GetPath(key);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            Write(stream, intent);
            stream.Flush(true);
        }
        File.Move(temporaryPath, path, true);
    }

    private static void Write(Stream stream, YouTubeIntent intent)
        => System.Text.Json.JsonSerializer.Serialize(stream, intent);

    private static YouTubeIntent Read(string path, string key)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<YouTubeIntent>(File.ReadAllText(path))
                ?? new YouTubeIntent(key, YouTubeIntentState.Unknown);
        }
        catch (Exception exception) when (exception is IOException or System.Text.Json.JsonException)
        {
            return new YouTubeIntent(key, YouTubeIntentState.Unknown);
        }
    }
}
