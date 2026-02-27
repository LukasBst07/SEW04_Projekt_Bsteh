namespace SEW04_Projekt_Bsteh.Models
{
    // Welche Gebaeude hat der Spieler + Upgrade-Stufen
    public class UserBuilding
    {
        public int Id { get; set; }

        public int FarmId { get; set; }
        public Farm Farm { get; set; } = null!;

        public int BuildingId { get; set; }
        public Building Building { get; set; } = null!;

        // Gekauft/freigeschaltet?
        public bool IsUnlocked { get; set; } = false;

        // Upgrade: Produktionsrate
        public int ProductionLevel { get; set; } = 0;

        // Upgrade: Effizienz (weniger Input)
        public int EfficiencyLevel { get; set; } = 0;

        // Upgrade: Lagerkapazitaet
        public int CapacityLevel { get; set; } = 0;
    }
}