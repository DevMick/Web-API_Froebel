using InstitutFroebel.Core.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstitutFroebel.Core.Entities.School
{
    public class Paiement : TenantEntity
    {
        public int EnfantId { get; set; }
        public string TypePaiement { get; set; } = string.Empty; // scolarite, cantine, transport, activite
        public decimal Montant { get; set; }
        public DateTime DatePaiement { get; set; }
        public DateTime DateEcheance { get; set; }
        public string ModePaiement { get; set; } = string.Empty; // especes, cheque, virement, mobile_money
        public string Statut { get; set; } = "en_attente"; // en_attente, paye, retard, annule
        public string? NumeroPiece { get; set; } // Numéro chèque, référence virement
        public string? Description { get; set; }
        public string Trimestre { get; set; } = string.Empty;
        public string AnneeScolaire { get; set; } = string.Empty;

        // Relations
        public virtual Enfant Enfant { get; set; } = null!;
    }
}
