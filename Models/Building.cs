using System.ComponentModel.DataAnnotations;

namespace SEW04_Projekt_Bsteh.Models
{
    // Stammdaten: Gebaeudetypen (Feld, Muehle, Baeckerei)
    public class Building
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        // Kaufpreis
        public decimal BaseCost { get; set; }

        // Produktion pro Sekunde (Basiswert)
        public double BaseProductionRate { get; set; }

        // Input-Ressource (null bei Feld, das braucht keinen Input)
        public int? InputResourceId { get; set; }
        public Resource? InputResource { get; set; }

        // Output-Ressource
        public int OutputResourceId { get; set; }
        public Resource OutputResource { get; set; } = null!;

        // Wie viel Input fuer 1 Output (z.B. 2 Weizen = 1 Mehl)
        public double InputPerOutput { get; set; }
    }
}