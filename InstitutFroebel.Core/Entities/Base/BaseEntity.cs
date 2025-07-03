using InstitutFroebel.Core.Entities.School;
using InstitutFroebel.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstitutFroebel.Core.Entities.Base
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? CreatedById { get; set; }
        public string? UpdatedById { get; set; }
    }

    public abstract class TenantEntity : BaseEntity, ITenantEntity
    {
        public int EcoleId { get; set; }
        public virtual Ecole Ecole { get; set; } = null!;
    }
}