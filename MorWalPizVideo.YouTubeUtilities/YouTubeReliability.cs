namespace MorWalPizVideo.YouTubeUtilities;

using Google;
using Google.Apis;
using System.Net;

public sealed record YouTubeRetryOptions
{
    public int MaxAttempts { get; init; } = 3;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(10);
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);
}

public enum YouTubeErrorKind
{
    Cancellation,
    Timeout,
    Transient,
    Permanent
}

public sealed record YouTubeOperationError(YouTubeErrorKind Kind, string Message, Exception? Exception = null);

public static class YouTubeErrorClassifier
{
    public static YouTubeErrorKind Classify(Exception exception, CancellationToken cancellationToken = default)
    {
        if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
            return YouTubeErrorKind.Cancellation;
        if (exception is TimeoutException || exception is OperationCanceledException)
            return YouTubeErrorKind.Timeout;
        if (exception is GoogleApiException googleException &&
            googleException.HttpStatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests or
            HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout)
            return YouTubeErrorKind.Transient;
        if (exception is IOException or HttpRequestException)
            return YouTubeErrorKind.Transient;
        return YouTubeErrorKind.Permanent;
    }
}

public sealed class YouTubeOperationExecutor(YouTubeRetryOptions options)
{
    public async Task<T> ExecuteAsync<T>(
        string operationKey,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKey);
        ArgumentNullException.ThrowIfNull(operation);
        var attempts = Math.Max(1, options.MaxAttempts);

        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(options.Timeout);
            try
            {
                return await operation(timeout.Token).WaitAsync(timeout.Token);
            }
            catch (Exception exception) when (attempt < attempts && IsRetryable(exception, cancellationToken))
            {
                var exponentialDelay = TimeSpan.FromMilliseconds(
                    options.InitialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                await Task.Delay(exponentialDelay <= options.MaxDelay ? exponentialDelay : options.MaxDelay, cancellationToken);
            }
        }
    }

    public static bool IsRetryable(Exception exception, CancellationToken cancellationToken = default)
    {
        var kind = YouTubeErrorClassifier.Classify(exception, cancellationToken);
        return kind is YouTubeErrorKind.Timeout or YouTubeErrorKind.Transient;
    }
}
