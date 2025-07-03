using FluentValidation;
using InstitutFroebel.API.DTOs.Auth;

namespace InstitutFroebel.API.Validators
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.SchoolId)
                .GreaterThan(0).WithMessage("L'identifiant de l'école doit être supérieur à 0");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("L'email est requis")
                .EmailAddress().WithMessage("Format d'email invalide")
                .MaximumLength(255).WithMessage("L'email ne peut pas dépasser 255 caractères");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Le mot de passe est requis")
                .MinimumLength(6).WithMessage("Le mot de passe doit contenir au moins 6 caractères")
                .MaximumLength(100).WithMessage("Le mot de passe ne peut pas dépasser 100 caractères");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Les mots de passe ne correspondent pas");

            RuleFor(x => x.Nom)
                .NotEmpty().WithMessage("Le nom est requis")
                .MaximumLength(100).WithMessage("Le nom ne peut pas dépasser 100 caractères");

            RuleFor(x => x.Prenom)
                .NotEmpty().WithMessage("Le prénom est requis")
                .MaximumLength(100).WithMessage("Le prénom ne peut pas dépasser 100 caractères");

            RuleFor(x => x.Telephone)
                .MaximumLength(20).WithMessage("Le téléphone ne peut pas dépasser 20 caractères")
                .When(x => !string.IsNullOrEmpty(x.Telephone));

            RuleFor(x => x.Adresse)
                .MaximumLength(500).WithMessage("L'adresse ne peut pas dépasser 500 caractères")
                .When(x => !string.IsNullOrEmpty(x.Adresse));
        }
    }
}