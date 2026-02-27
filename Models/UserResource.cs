namespace SEW04_Projekt_Bsteh.Models
{
    // Aktueller Ressourcenbestand pro Spieler
    public class UserResource
    {
        public int Id { get; set; }

        public int FarmId { get; set; }
        public Farm Farm { get; set; } = null!;

        public int ResourceId { get; set; }
        public Resource Resource { get; set; } = null!;

        // Aktuelle Menge im Lager
        public double Amount { get; set; } = 0;

        // Max Lagerkapazitaet
        public double MaxStorage { get; set; } = 100;
    }
}