using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstitutFroebel.Core.Entities.School;
using InstitutFroebel.Core.Interfaces;

namespace InstitutFroebel.Core.Entities.Base
{
    public interface ITenantEntity
    {
        int EcoleId { get; set; }
    }

    public interface IAuditable
    {
        DateTime CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
        string? CreatedById { get; set; }
        string? UpdatedById { get; set; }
    }
}