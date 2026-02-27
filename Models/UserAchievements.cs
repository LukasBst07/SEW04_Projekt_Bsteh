namespace SEW04_Projekt_Bsteh.Models
{
    // Welche Achievements hat der Spieler freigeschaltet
    public class UserAchievement
    {
        public int Id { get; set; }

        public int FarmId { get; set; }
        public Farm Farm { get; set; } = null!;

        public int AchievementId { get; set; }
        public Achievement Achievement { get; set; } = null!;

        // Wann freigeschaltet
        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
    }
}