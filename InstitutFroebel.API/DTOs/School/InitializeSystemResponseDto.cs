namespace InstitutFroebel.API.DTOs.School
{
    public class InitializeSystemResponseDto
    {
        public int SchoolId { get; set; }
        public string SchoolCode { get; set; } = string.Empty;
        public string SchoolNom { get; set; } = string.Empty;
        public string SuperAdminId { get; set; } = string.Empty;
        public string SuperAdminEmail { get; set; } = string.Empty;
        public string SuperAdminNom { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime TokenExpiration { get; set; }
    }
} 