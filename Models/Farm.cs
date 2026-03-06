using System.ComponentModel.DataAnnotations;

namespace SEW04_Projekt_Bsteh.Models
{
    public class Farm
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public decimal Money { get; set; } = 100m;
        public double RebirthMultiplier { get; set; } = 1.0;
        public int RebirthCount { get; set; } = 0;
        public DateTime LastCalculated { get; set; } = DateTime.UtcNow;

        public bool AllocationUnlocked { get; set; } = false;
        public double AchievementProductionBonus { get; set; } = 0;
        public double AchievementSellBonus { get; set; } = 0;
        public double AchievementUpgradeDiscount { get; set; } = 0;
        public double AchievementStorageBonus { get; set; } = 0;
    }
}