using System.ComponentModel.DataAnnotations;

namespace SEW04_Projekt_Bsteh.Models
{
    // Stammdaten: Achievements
    public class Achievement
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string Description { get; set; } = string.Empty;

        // Gem-Belohnung
        [Range(0, 1000)]
        public int GemReward { get; set; } = 0;
    }
}