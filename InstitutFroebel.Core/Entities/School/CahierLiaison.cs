using InstitutFroebel.Core.Entities.Base;
using InstitutFroebel.Core.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstitutFroebel.Core.Entities.School
{
    public class CahierLiaison : TenantEntity
    {
        public int EnfantId { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "info"; // info, devoirs, comportement, sante, felicitation, sanction
        public string? Fichiers { get; set; } // JSON des fichiers attachés
        public bool LuParParent { get; set; } = false;
        public DateTime? DateLecture { get; set; }
        public bool ReponseRequise { get; set; } = false;
        public string? ReponseParent { get; set; }
        public DateTime? DateReponse { get; set; }

        // Relations
        public virtual Enfant Enfant { get; set; } = null!;
        public virtual ApplicationUser CreatedBy { get; set; } = null!;
    }
}
