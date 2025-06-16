using System.Collections.Generic;
using System.Threading.Tasks;
using MorWalPiz.VideoImporter.Models;

namespace MorWalPiz.VideoImporter.Services
{
    public interface ITenantService
    {
        Task<List<Tenant>> GetAllTenantsAsync();
        Task<List<Tenant>> GetActiveTenantsAsync();
        Task<Tenant> GetTenantByIdAsync(int id);
        Task<Tenant> CreateTenantAsync(string name);
        Task<Tenant> UpdateTenantAsync(Tenant tenant);
        Task DeleteTenantAsync(int id);
        Task<bool> TenantExistsAsync(string name);
        Task<(bool isValid, string errorMessage)> ValidateTenantAsync(Tenant tenant);
    }
}
