using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SEW04_Projekt_Bsteh.Models
{
    // Zentrale Spieler-Entitaet, eine Farm pro User
    public class Farm
    {
        public int Id { get; set; }

        // Verknuepfung zu Identity User
        [Required]
        public string UserId { get; set; } = string.Empty;
        public IdentityUser User { get; set; } = null!;

        // Geld
        public decimal Money { get; set; } = 100m;

        // Gems fuer Freischaltungen
        public int Gems { get; set; } = 0;

        // Rebirth-Multiplikator (1.0 = kein Bonus)
        public double RebirthMultiplier { get; set; } = 1.0;

        // Anzahl Rebirths
        public int RebirthCount { get; set; } = 0;

        // Letzter Berechnungszeitpunkt fuer Offline-Progress
        public DateTime LastCalculated { get; set; } = DateTime.UtcNow;
    }
}