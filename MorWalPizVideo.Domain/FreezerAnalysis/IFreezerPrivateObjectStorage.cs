namespace MorWalPizVideo.Domain.FreezerAnalysis;

public sealed record PrivateObjectReference(
    string Key,
    string Sha256,
    long Length,
    string ContentType);

public interface IFreezerPrivateObjectStorage
{
    Task<PrivateObjectReference> PutAsync(
        string category,
        string sha256,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken);

    Task<byte[]> GetAsync(
        PrivateObjectReference reference,
        CancellationToken cancellationToken);
}
