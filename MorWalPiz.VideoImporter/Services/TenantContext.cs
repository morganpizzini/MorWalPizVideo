using System;
using System.IO;
using System.Text.Json;

namespace MorWalPiz.VideoImporter.Services
{
    public class TenantContext : ITenantContext
    {
        private const string SETTINGS_FILE = "tenant-settings.json";
        private int _currentTenantId = 1; // Default tenant ID
        private string _currentTenantName = "MorWalPiz";

        public int CurrentTenantId => _currentTenantId;
        public string CurrentTenantName => _currentTenantName;

        public event EventHandler<TenantChangedEventArgs> TenantChanged;

        public TenantContext()
        {
            LoadSettings();
        }

        public void SetCurrentTenant(int tenantId, string tenantName)
        {
            if (_currentTenantId != tenantId)
            {
                _currentTenantId = tenantId;
                _currentTenantName = tenantName;
                SaveSettings();
                TenantChanged?.Invoke(this, new TenantChangedEventArgs(tenantId, tenantName));
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SETTINGS_FILE))
                {
                    var json = File.ReadAllText(SETTINGS_FILE);
                    var settings = JsonSerializer.Deserialize<TenantSettings>(json);
                    if (settings != null)
                    {
                        _currentTenantId = settings.CurrentTenantId;
                        _currentTenantName = settings.CurrentTenantName ?? "MorWalPiz";
                    }
                }
            }
            catch (Exception)
            {
                // If loading fails, keep default values
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new TenantSettings
                {
                    CurrentTenantId = _currentTenantId,
                    CurrentTenantName = _currentTenantName
                };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SETTINGS_FILE, json);
            }
            catch (Exception)
            {
                // If saving fails, ignore for now
            }
        }

        private class TenantSettings
        {
            public int CurrentTenantId { get; set; }
            public string CurrentTenantName { get; set; } = string.Empty;
        }
    }
}
