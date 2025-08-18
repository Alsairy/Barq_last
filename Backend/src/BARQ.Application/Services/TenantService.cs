using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BARQ.Application.Services
{
    public class TenantService : ITenantService
    {
        private readonly BarqDbContext _context;

        public TenantService(BarqDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<TenantDto>> GetTenantsAsync(ListRequest request)
        {
            var query = _context.Tenants.AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(t => t.Name.Contains(request.SearchTerm) || 
                                        t.DisplayName.Contains(request.SearchTerm) ||
                                        (t.Description != null && t.Description.Contains(request.SearchTerm)));
            }

            var totalCount = await query.CountAsync();

            var tenants = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new TenantDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    DisplayName = t.DisplayName,
                    Description = t.Description,
                    IsActive = t.IsActive,
                    ContactEmail = t.ContactEmail,
                    ContactPhone = t.ContactPhone,
                    Address = t.Address,
                    SubscriptionStartDate = t.SubscriptionStartDate,
                    SubscriptionEndDate = t.SubscriptionEndDate,
                    SubscriptionTier = t.SubscriptionTier,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync();

            return new PagedResult<TenantDto>
            {
                Items = tenants,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<TenantDto?> GetTenantByIdAsync(Guid id)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null) return null;

            return new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                DisplayName = tenant.DisplayName,
                Description = tenant.Description,
                IsActive = tenant.IsActive,
                ContactEmail = tenant.ContactEmail,
                ContactPhone = tenant.ContactPhone,
                Address = tenant.Address,
                SubscriptionStartDate = tenant.SubscriptionStartDate,
                SubscriptionEndDate = tenant.SubscriptionEndDate,
                SubscriptionTier = tenant.SubscriptionTier,
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt
            };
        }

        public async Task<TenantDto?> GetTenantByDomainAsync(string domain)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.DisplayName == domain);

            if (tenant == null) return null;

            return new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                DisplayName = tenant.DisplayName,
                Description = tenant.Description,
                IsActive = tenant.IsActive,
                ContactEmail = tenant.ContactEmail,
                ContactPhone = tenant.ContactPhone,
                Address = tenant.Address,
                SubscriptionStartDate = tenant.SubscriptionStartDate,
                SubscriptionEndDate = tenant.SubscriptionEndDate,
                SubscriptionTier = tenant.SubscriptionTier,
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt
            };
        }

        public async Task<TenantDto?> GetTenantByNameAsync(string name)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Name == name);

            if (tenant == null) return null;

            return new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                DisplayName = tenant.DisplayName,
                Description = tenant.Description,
                IsActive = tenant.IsActive,
                ContactEmail = tenant.ContactEmail,
                ContactPhone = tenant.ContactPhone,
                Address = tenant.Address,
                SubscriptionStartDate = tenant.SubscriptionStartDate,
                SubscriptionEndDate = tenant.SubscriptionEndDate,
                SubscriptionTier = tenant.SubscriptionTier,
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt
            };
        }

        public async Task<TenantDto> CreateTenantAsync(CreateTenantRequest request)
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = request.Description,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                Address = request.Address,
                SubscriptionTier = request.SubscriptionTier,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Version = 1
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            return new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                DisplayName = tenant.DisplayName,
                Description = tenant.Description,
                IsActive = tenant.IsActive,
                ContactEmail = tenant.ContactEmail,
                ContactPhone = tenant.ContactPhone,
                Address = tenant.Address,
                SubscriptionStartDate = tenant.SubscriptionStartDate,
                SubscriptionEndDate = tenant.SubscriptionEndDate,
                SubscriptionTier = tenant.SubscriptionTier,
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt
            };
        }

        public async Task<TenantDto> UpdateTenantAsync(Guid id, UpdateTenantRequest request)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null)
                throw new ArgumentException("Tenant not found");

            tenant.DisplayName = request.DisplayName;
            tenant.Description = request.Description;
            tenant.ContactEmail = request.ContactEmail;
            tenant.ContactPhone = request.ContactPhone;
            tenant.Address = request.Address;
            tenant.IsActive = request.IsActive;
            tenant.SubscriptionTier = request.SubscriptionTier;
            tenant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                DisplayName = tenant.DisplayName,
                Description = tenant.Description,
                IsActive = tenant.IsActive,
                ContactEmail = tenant.ContactEmail,
                ContactPhone = tenant.ContactPhone,
                Address = tenant.Address,
                SubscriptionStartDate = tenant.SubscriptionStartDate,
                SubscriptionEndDate = tenant.SubscriptionEndDate,
                SubscriptionTier = tenant.SubscriptionTier,
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt
            };
        }

        public async Task<bool> ActivateTenantAsync(Guid id)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null) return false;

            tenant.IsActive = true;
            tenant.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeactivateTenantAsync(Guid id)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null) return false;

            tenant.IsActive = false;
            tenant.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteTenantAsync(Guid id)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null) return false;

            tenant.IsDeleted = true;
            tenant.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
    }

}
