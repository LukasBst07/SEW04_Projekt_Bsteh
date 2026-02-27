using System.ComponentModel.DataAnnotations;

namespace SEW04_Projekt_Bsteh.Models
{
    // Prozentuale Verteilung: wie viel wird verkauft vs. weiterverarbeitet
    public class ResourceAllocation
    {
        public int Id { get; set; }

        public int FarmId { get; set; }
        public Farm Farm { get; set; } = null!;

        public int ResourceId { get; set; }
        public Resource Resource { get; set; } = null!;

        // Prozent zum Verkauf (0-100), Rest geht zur Weiterverarbeitung
        [Range(0, 100)]
        public int SellPercentage { get; set; } = 100;
    }
}