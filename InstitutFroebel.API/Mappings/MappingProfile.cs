using AutoMapper;
using InstitutFroebel.Core.Entities.Identity;
using InstitutFroebel.Core.Entities.School;
using InstitutFroebel.API.DTOs.Auth;
using InstitutFroebel.API.DTOs.User;
using InstitutFroebel.API.DTOs.School;
using InstitutFroebel.API.DTOs.Student;
using InstitutFroebel.API.DTOs.Announcement;

namespace InstitutFroebel.API.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User Mappings
            CreateMap<ApplicationUser, UserDto>()
                .ForMember(dest => dest.NomComplet, opt => opt.MapFrom(src => src.NomComplet))
                .ForMember(dest => dest.EcoleNom, opt => opt.MapFrom(src => src.Ecole != null ? src.Ecole.Nom : null))
                .ForMember(dest => dest.EcoleCode, opt => opt.MapFrom(src => src.Ecole != null ? src.Ecole.Code : null))
                .ForMember(dest => dest.Roles, opt => opt.Ignore()) // Sera mappé séparément
                .ForMember(dest => dest.Enfants, opt => opt.MapFrom(src => src.EnfantsAsParent.Select(pe => pe.Enfant)));

            CreateMap<RegisterDto, ApplicationUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EcoleId, opt => opt.Ignore()) // Sera défini séparément
                .ForMember(dest => dest.Ecole, opt => opt.Ignore())
                .ForMember(dest => dest.EnfantsAsParent, opt => opt.Ignore())
                .ForMember(dest => dest.EnfantsAsTeacher, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            // Mapping pour UpdateUserDto -> ApplicationUser (seulement les champs modifiables)
            CreateMap<UpdateUserDto, ApplicationUser>()
                .ForMember(dest => dest.Nom, opt => opt.MapFrom(src => src.Nom))
                .ForMember(dest => dest.Prenom, opt => opt.MapFrom(src => src.Prenom))
                .ForMember(dest => dest.Telephone, opt => opt.MapFrom(src => src.Telephone))
                .ForMember(dest => dest.Adresse, opt => opt.MapFrom(src => src.Adresse))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserName, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.EcoleId, opt => opt.Ignore())
                .ForMember(dest => dest.Ecole, opt => opt.Ignore())
                .ForMember(dest => dest.EnfantsAsParent, opt => opt.Ignore())
                .ForMember(dest => dest.EnfantsAsTeacher, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // School Mappings
            CreateMap<Ecole, SchoolDto>();

            CreateMap<CreateSchoolDto, Ecole>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Users, opt => opt.Ignore())
                .ForMember(dest => dest.Enfants, opt => opt.Ignore())
                .ForMember(dest => dest.Annonces, opt => opt.Ignore())
                .ForMember(dest => dest.Activites, opt => opt.Ignore());

            // Student Mappings
            CreateMap<Enfant, EnfantDto>()
                .ForMember(dest => dest.ParentNom, opt => opt.Ignore()); // Will be set manually in controllers

            // Announcement Mappings
            CreateMap<Annonce, AnnonceDto>()
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedBy != null ? src.CreatedBy.NomComplet : null));
        }
    }
}