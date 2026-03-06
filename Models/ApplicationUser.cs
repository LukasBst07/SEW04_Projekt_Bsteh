using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SEW04_Projekt_Bsteh.Models
{
    // Eigener User der von IdentityUser erbt
    // Damit koennen wir eigene Felder hinzufuegen
    public class ApplicationUser : IdentityUser
    {
        // Anzeigename im Spiel
        [Required]
        [StringLength(50)]
        public string DisplayName { get; set; } = string.Empty;

        // Wann wurde der Account erstellt
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}