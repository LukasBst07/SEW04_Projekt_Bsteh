using System.ComponentModel.DataAnnotations;

namespace SEW04_Projekt_Bsteh.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-Mail ist erforderlich.")]
        [EmailAddress(ErrorMessage = "Ungueltige E-Mail-Adresse.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Passwort ist erforderlich.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Anzeigename ist erforderlich.")]
        [StringLength(50, ErrorMessage = "Maximal 50 Zeichen.")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-Mail ist erforderlich.")]
        [EmailAddress(ErrorMessage = "Ungueltige E-Mail-Adresse.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Passwort ist erforderlich.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "Mindestens 4 Zeichen.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Passwort bestaetigen ist erforderlich.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwoerter stimmen nicht ueberein.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}