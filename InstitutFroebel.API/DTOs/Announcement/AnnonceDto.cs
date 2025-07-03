namespace InstitutFroebel.API.DTOs.Announcement
{
    public class AnnonceDto
    {
        public int Id { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string Contenu { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime DatePublication { get; set; }
        public DateTime? DateExpiration { get; set; }
        public bool Visible { get; set; }
        public string? ClasseCible { get; set; }
        public string CreatedById { get; set; } = string.Empty;
        public string? CreatedByName { get; set; }
        public bool IsExpired => DateExpiration.HasValue && DateExpiration.Value < DateTime.UtcNow;
        public bool IsActive => Visible && !IsExpired;
    }
}