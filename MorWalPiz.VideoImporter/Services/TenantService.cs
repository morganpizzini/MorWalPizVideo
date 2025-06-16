using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MorWalPiz.VideoImporter.Data;
using MorWalPiz.VideoImporter.Models;

namespace MorWalPiz.VideoImporter.Services
{
    public class TenantService : ITenantService
    {
        private readonly DatabaseService _databaseService;

        public TenantService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<List<Tenant>> GetAllTenantsAsync()
        {
            using var context = _databaseService.CreateContext();
            // For tenants, we don't apply tenant filtering since we need to see all tenants
            return await context.Set<Tenant>().OrderBy(t => t.Name).ToListAsync();
        }

        public async Task<List<Tenant>> GetActiveTenantsAsync()
        {
            using var context = _databaseService.CreateContext();
            return await context.Set<Tenant>()
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<Tenant> GetTenantByIdAsync(int id)
        {
            using var context = _databaseService.CreateContext();
            return await context.Set<Tenant>().FindAsync(id);
        }

        public async Task<Tenant> CreateTenantAsync(string name)
        {
            var validation = await ValidateTenantAsync(new Tenant { Name = name });
            if (!validation.isValid)
            {
                throw new ArgumentException(validation.errorMessage);
            }

            var tenant = new Tenant
            {
                Name = name,
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            using var context = _databaseService.CreateContext();
            context.Set<Tenant>().Add(tenant);
            await context.SaveChangesAsync();
            
            return tenant;
        }

        public async Task<Tenant> UpdateTenantAsync(Tenant tenant)
        {
            var validation = await ValidateTenantAsync(tenant);
            if (!validation.isValid)
            {
                throw new ArgumentException(validation.errorMessage);
            }

            using var context = _databaseService.CreateContext();
            context.Set<Tenant>().Update(tenant);
            await context.SaveChangesAsync();
            
            return tenant;
        }

        public async Task DeleteTenantAsync(int id)
        {
            using var context = _databaseService.CreateContext();
            var tenant = await context.Set<Tenant>().FindAsync(id);
            if (tenant != null)
            {
                // Check if this is the only tenant
                var tenantCount = await context.Set<Tenant>().CountAsync();
                if (tenantCount <= 1)
                {
                    throw new InvalidOperationException("Cannot delete the last tenant.");
                }

                context.Set<Tenant>().Remove(tenant);
                await context.SaveChangesAsync();
            }
        }

        public async Task<bool> TenantExistsAsync(string name)
        {
            using var context = _databaseService.CreateContext();
            return await context.Set<Tenant>()
                .AnyAsync(t => t.Name.ToLower() == name.ToLower());
        }

        public async Task<(bool isValid, string errorMessage)> ValidateTenantAsync(Tenant tenant)
        {
            if (string.IsNullOrWhiteSpace(tenant.Name))
            {
                return (false, "Il nome del tenant è obbligatorio.");
            }

            if (tenant.Name.Length > 100)
            {
                return (false, "Il nome del tenant non può superare i 100 caratteri.");
            }

            using var context = _databaseService.CreateContext();
            var existingTenant = await context.Set<Tenant>()
                .FirstOrDefaultAsync(t => t.Name.ToLower() == tenant.Name.ToLower() && t.Id != tenant.Id);

            if (existingTenant != null)
            {
                return (false, "Un tenant con questo nome esiste già.");
            }

            return (true, string.Empty);
        }
    }
}
