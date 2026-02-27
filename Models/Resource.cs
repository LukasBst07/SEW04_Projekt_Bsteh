using System.ComponentModel.DataAnnotations;

namespace SEW04_Projekt_Bsteh.Models
{
    // Stammdaten: Ressourcentypen (Weizen, Mehl, Brot)
    public class Resource
    {
        public int Id { get; set; }

        // Name z.B. "Weizen", "Mehl", "Brot"
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        // Verkaufspreis pro Einheit
        [Range(0, 1_000_000)]
        public decimal SellPrice { get; set; }

        // Position in der Kette (0=Weizen, 1=Mehl, 2=Brot)
        public int ChainOrder { get; set; }
    }
}