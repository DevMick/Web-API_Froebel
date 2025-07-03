using Microsoft.EntityFrameworkCore;
using InstitutFroebel.Core.Interfaces;
using InstitutFroebel.Core.Entities.School;
using InstitutFroebel.API.Data;

namespace InstitutFroebel.API.Services
{
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;
        private string? _currentTenantCode;
        private int? _currentTenantId;

        public TenantService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public string? GetCurrentTenantCode()
        {
            if (_currentTenantCode != null)
                return _currentTenantCode;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            // Essayer d'abord les claims JWT
            var tenantClaim = httpContext.User.FindFirst("school_code");
            if (tenantClaim != null)
            {
                _currentTenantCode = tenantClaim.Value;
                return _currentTenantCode;
            }

            // Puis essayer le header
            if (httpContext.Request.Headers.TryGetValue("X-School-Code", out var headerValue))
            {
                _currentTenantCode = headerValue.FirstOrDefault();
                return _currentTenantCode;
            }

            return null;
        }

        public int? GetCurrentTenantId()
        {
            if (_currentTenantId.HasValue)
                return _currentTenantId;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            // Essayer d'abord les claims JWT
            var tenantIdClaim = httpContext.User.FindFirst("school_id");
            if (tenantIdClaim != null && int.TryParse(tenantIdClaim.Value, out var claimTenantId))
            {
                _currentTenantId = claimTenantId;
                return _currentTenantId;
            }

            // Sinon, résoudre à partir du code
            var tenantCode = GetCurrentTenantCode();
            if (!string.IsNullOrEmpty(tenantCode))
            {
                var school = GetSchoolByCodeAsync(tenantCode).Result;
                if (school != null)
                {
                    _currentTenantId = school.Id;
                    return _currentTenantId;
                }
            }

            return null;
        }

        public async Task<Ecole?> GetCurrentSchoolAsync()
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
                return null;

            return await _context.Ecoles
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == tenantId.Value);
        }

        public async Task<Ecole?> GetSchoolByCodeAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
                return null;

            return await _context.Ecoles
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Code == code);
        }

        public void SetCurrentTenant(string tenantCode, int tenantId)
        {
            _currentTenantCode = tenantCode;
            _currentTenantId = tenantId;
        }
    }
}