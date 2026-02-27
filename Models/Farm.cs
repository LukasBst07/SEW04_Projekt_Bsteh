using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SEW04_Projekt_Bsteh.Models
{
    // Zentrale Spieler-Entitaet, eine Farm pro User
    public class Farm
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public IdentityUser User { get; set; } = null!;

        // Geld
        public decimal Money { get; set; } = 100m;

        // Rebirth-Multiplikator (1.0 = kein Bonus)
        public double RebirthMultiplier { get; set; } = 1.0;

        // Anzahl Rebirths
        public int RebirthCount { get; set; } = 0;

        // Letzter Berechnungszeitpunkt fuer Offline-Progress
        public DateTime LastCalculated { get; set; } = DateTime.UtcNow;

        // === Achievement-Boni (werden bei Rebirth resetted) ===

        // Ist die Ressourcenverteilung freigeschaltet?
        public bool AllocationUnlocked { get; set; } = false;

        // Permanenter Produktionsbonus durch Achievements (z.B. 0.1 = +10%)
        public double AchievementProductionBonus { get; set; } = 0;

        // Permanenter Verkaufspreisbonus (z.B. 0.2 = +20%)
        public double AchievementSellBonus { get; set; } = 0;

        // Permanenter Upgrade-Kostenrabatt (z.B. 0.15 = -15%)
        public double AchievementUpgradeDiscount { get; set; } = 0;

        // Permanenter Lagerbonus (z.B. 0.5 = +50%)
        public double AchievementStorageBonus { get; set; } = 0;
    }
}