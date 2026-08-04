using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace MorWalPizVideo.BackOffice.Services;

public sealed record BackendProblem(
    DateTimeOffset TimestampUtc,
    string Category,
    string Message,
    IReadOnlyDictionary<string, string?> Properties);

public sealed class DiagnosticsProblemStore
{
    private const int MaximumEntries = 100;
    private readonly ConcurrentQueue<BackendProblem> _problems = new();

    public void Record(string category, string message, IReadOnlyDictionary<string, string?> properties)
    {
        var safeProperties = properties.ToDictionary(
            item => item.Key,
            item => DiagnosticsRedactor.Redact(item.Key, item.Value));
        _problems.Enqueue(new BackendProblem(
            DateTimeOffset.UtcNow,
            DiagnosticsRedactor.Redact(category) ?? string.Empty,
            DiagnosticsRedactor.Redact(message) ?? string.Empty,
            safeProperties));
        while (_problems.Count > MaximumEntries && _problems.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<BackendProblem> GetRecent(int limit)
        => _problems.Reverse().Take(Math.Clamp(limit, 1, MaximumEntries)).ToArray();
}

public sealed class DiagnosticsLoggerProvider : ILoggerProvider
{
    private readonly DiagnosticsProblemStore _store;

    public DiagnosticsLoggerProvider(DiagnosticsProblemStore store) => _store = store;

    public ILogger CreateLogger(string categoryName) => new DiagnosticsLogger(categoryName, _store);

    public void Dispose()
    {
    }

    private sealed class DiagnosticsLogger(string categoryName, DiagnosticsProblemStore store) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var properties = state is IEnumerable<KeyValuePair<string, object?>> structuredState
                ? structuredState.ToDictionary(item => item.Key, item => item.Value?.ToString())
                : new Dictionary<string, string?>();
            properties["eventId"] = eventId.Id.ToString();
            properties["exceptionType"] = exception?.GetType().FullName;
            store.Record(categoryName, formatter(state, exception), properties);
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}

public static partial class DiagnosticsRedactor
{
    private static readonly Regex SensitiveKey = new(
        "(?:authorization|cookie|password|passwd|secret|token|api[-_]?key|connection[-_]?string|access[-_]?key|client[-_]?secret)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [GeneratedRegex(@"(?i)\b(bearer|basic)\s+[^\s,;]+")]
    private static partial Regex AuthorizationValue();

    [GeneratedRegex(@"(?i)\b(authorization|cookie|set-cookie)\s*[:=]\s*[^,;\r\n]+")]
    private static partial Regex HeaderValue();

    [GeneratedRegex(@"(?i)\b(password|passwd|secret|token|api[-_]?key|connection[-_]?string|access[-_]?key|client[-_]?secret)\s*[:=]\s*[^,;\r\n]+")]
    private static partial Regex SensitiveAssignment();

    [GeneratedRegex(@"(?i)\b(mongodb(?:\+srv)?|sqlserver|postgres(?:ql)?|mysql)://[^@\s]+@")]
    private static partial Regex ConnectionCredential();

    public static string? Redact(string? value)
        => value is null
            ? null
            : ConnectionCredential().Replace(
                SensitiveAssignment().Replace(
                    HeaderValue().Replace(AuthorizationValue().Replace(value, "$1 [REDACTED]"), "$1: [REDACTED]"),
                    "$1=[REDACTED]"),
                "$1://[REDACTED]@");

    public static string? Redact(string key, string? value)
        => SensitiveKey.IsMatch(key) ? "[REDACTED]" : Redact(value);
}