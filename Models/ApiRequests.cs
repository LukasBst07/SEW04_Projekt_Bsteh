namespace SEW04_Projekt_Bsteh.Models
{
    // Request-Models fuer AJAX-Endpoints
    public class UpgradeRequest
    {
        public int BuildingId { get; set; }
        public string UpgradeType { get; set; } = string.Empty;
    }

    public class AllocationRequest
    {
        public int ResourceId { get; set; }
        public int SellPercentage { get; set; }
    }

    public class SellRequest
    {
        public int ResourceId { get; set; }
        public double Amount { get; set; }
    }
}