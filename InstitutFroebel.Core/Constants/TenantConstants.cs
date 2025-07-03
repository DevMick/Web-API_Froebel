using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstitutFroebel.Core.Constants
{
    public static class TenantConstants
    {
        public const string TenantHeaderName = "X-School-Code";
        public const string TenantClaimType = "school_code";
        public const string TenantIdClaimType = "school_id";
        public const string DefaultTenantCode = "DEMO_SCHOOL";
    }
}
