using System.ComponentModel.DataAnnotations;

namespace SEW04_Projekt_Bsteh.Models
{
    // Stammdaten: Achievements mit direkten Gameplay-Boni
    public class Achievement
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string Description { get; set; } = string.Empty;

        // Welchen Bonus gibt dieses Achievement?
        // z.B. "UnlockAllocation", "StorageBoost", "ProductionBoost" etc.
        [Required]
        [StringLength(50)]
        public string BonusType { get; set; } = string.Empty;

        // Staerke des Bonus (z.B. 0.1 = +10%, 0.5 = +50%)
        public double BonusValue { get; set; } = 0;

        // Beschreibung des Bonus fuer die UI
        [StringLength(200)]
        public string BonusDescription { get; set; } = string.Empty;
    }
}