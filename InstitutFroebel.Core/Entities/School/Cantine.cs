using InstitutFroebel.Core.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstitutFroebel.Core.Entities.School
{
    public class Cantine : TenantEntity
    {
        public DateTime DateMenu { get; set; }
        public string TypeRepas { get; set; } = "dejeuner"; // dejeuner, gouter
        public string Menu { get; set; } = string.Empty;
        public decimal? Prix { get; set; }
        public string? Semaine { get; set; }
    }
}
