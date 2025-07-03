using InstitutFroebel.Core.Entities.School;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstitutFroebel.Core.Interfaces
{
    public interface ITenantService
    {
        string? GetCurrentTenantCode();
        int? GetCurrentTenantId();
        Task<Ecole?> GetCurrentSchoolAsync();
        Task<Ecole?> GetSchoolByCodeAsync(string code);
        void SetCurrentTenant(string tenantCode, int tenantId);
    }
}
