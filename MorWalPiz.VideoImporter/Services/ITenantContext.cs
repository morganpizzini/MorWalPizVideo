using System;

namespace MorWalPiz.VideoImporter.Services
{
    public interface ITenantContext
    {
        int CurrentTenantId { get; }
        string CurrentTenantName { get; }
        void SetCurrentTenant(int tenantId, string tenantName);
        event EventHandler<TenantChangedEventArgs> TenantChanged;
    }

    public class TenantChangedEventArgs : EventArgs
    {
        public int TenantId { get; set; }
        public string TenantName { get; set; }

        public TenantChangedEventArgs(int tenantId, string tenantName)
        {
            TenantId = tenantId;
            TenantName = tenantName;
        }
    }
}
