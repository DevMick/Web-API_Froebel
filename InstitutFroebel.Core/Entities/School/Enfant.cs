using InstitutFroebel.Core.Entities.Base;
using InstitutFroebel.Core.Entities.Identity;

namespace InstitutFroebel.Core.Entities.School
{
    public class Enfant : TenantEntity
    {
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public DateTime DateNaissance { get; set; }
        public string Sexe { get; set; } = string.Empty;

        // Informations scolaires
        public int? ClasseId { get; set; }
        public string AnneeScolaire { get; set; } = string.Empty; // AJOUTÉ
        public string Statut { get; set; } = "pre_inscrit";
        public DateTime? DateInscription { get; set; }

        // Cantine
        public bool UtiliseCantine { get; set; } = false;
        // SUPPRIMÉ: NumeroEtudiant, CantinePaye

        // Relations via tables de liaison - NOUVELLES
        public virtual ICollection<ParentEnfant> ParentsEnfants { get; set; } = new List<ParentEnfant>();
        public virtual ICollection<TeacherEnfant> TeachersEnfants { get; set; } = new List<TeacherEnfant>();

        // Relations directes
        public virtual Classe? Classe { get; set; }
        public virtual ICollection<Bulletin> Bulletins { get; set; } = new List<Bulletin>();
        public virtual ICollection<CahierLiaison> MessagesLiaison { get; set; } = new List<CahierLiaison>();
        // SUPPRIMÉ: Paiements
    }

    // Table de liaison Parent-Enfant
    public class ParentEnfant : TenantEntity
    {
        public string ParentId { get; set; } = string.Empty;
        public int EnfantId { get; set; }

        // Relations
        public virtual ApplicationUser Parent { get; set; } = null!;
        public virtual Enfant Enfant { get; set; } = null!;
    }

    // Table de liaison Teacher-Enfant
    public class TeacherEnfant : TenantEntity
    {
        public string TeacherId { get; set; } = string.Empty;
        public int EnfantId { get; set; }

        // Relations
        public virtual ApplicationUser Teacher { get; set; } = null!;
        public virtual Enfant Enfant { get; set; } = null!;
    }
}