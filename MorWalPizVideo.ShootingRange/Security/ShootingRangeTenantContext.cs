namespace MorWalPizVideo.ShootingRange.Security;

public sealed class ShootingRangeTenantContext(string? defaultTenantId = null)
{
    private readonly AsyncLocal<string?> currentTenantId = new();

    public string? TenantId => currentTenantId.Value ?? defaultTenantId;

    public IDisposable BeginScope(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        var previous = currentTenantId.Value;
        currentTenantId.Value = tenantId;
        return new TenantScope(this, previous);
    }

    private sealed class TenantScope(ShootingRangeTenantContext owner, string? previous) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed) return;
            owner.currentTenantId.Value = previous;
            disposed = true;
        }
    }
}
