using Microsoft.EntityFrameworkCore;
using SEW04_Projekt_Bsteh.Data;
using SEW04_Projekt_Bsteh.Models;

namespace SEW04_Projekt_Bsteh.Services
{
    // Prueft ob Achievements erreicht wurden und wendet Boni an
    public class AchievementService
    {
        private readonly ApplicationDbContext _db;

        public AchievementService(ApplicationDbContext db)
        {
            _db = db;
        }

        // Alle Achievements pruefen und neue freischalten
        public async Task CheckAchievements(int farmId)
        {
            var farm = await _db.Farms.FindAsync(farmId);
            if (farm == null) return;

            var unlockedIds = await _db.UserAchievements
                .Where(ua => ua.FarmId == farmId)
                .Select(ua => ua.AchievementId)
                .ToListAsync();

            var allAchievements = await _db.Achievements.ToListAsync();

            var userBuildings = await _db.UserBuildings
                .Include(ub => ub.Building)
                .Where(ub => ub.FarmId == farmId)
                .ToListAsync();

            var userResources = await _db.UserResources
                .Where(ur => ur.FarmId == farmId)
                .ToListAsync();

            foreach (var achievement in allAchievements)
            {
                // Schon freigeschaltet? Skip.
                if (unlockedIds.Contains(achievement.Id)) continue;

                bool earned = CheckCondition(achievement, farm, userBuildings, userResources);

                if (earned)
                {
                    // Achievement freischalten
                    _db.UserAchievements.Add(new UserAchievement
                    {
                        FarmId = farmId,
                        AchievementId = achievement.Id,
                        UnlockedAt = DateTime.UtcNow
                    });

                    // Bonus anwenden
                    ApplyBonus(farm, achievement);
                }
            }

            await _db.SaveChangesAsync();
        }

        private bool CheckCondition(Achievement achievement, Farm farm,
            List<UserBuilding> buildings, List<UserResource> resources)
        {
            return achievement.Name switch
            {
                "Muehlenbesitzer" =>
                    buildings.Any(b => b.Building.Name == "Muehle" && b.IsUnlocked),

                "Baeckermeister" =>
                    buildings.Any(b => b.Building.Name == "Baeckerei" && b.IsUnlocked),

                "Erste 1000 Muenzen" =>
                    farm.Money >= 1000,

                "Erste 10000 Muenzen" =>
                    farm.Money >= 10000,

                "Vollstaendige Kette" =>
                    buildings.Count(b => b.IsUnlocked) >= 3,

                "Lagermeister" =>
                    resources.Any(r => r.Amount >= r.MaxStorage * 0.99),

                "Upgrade-Anfaenger" =>
                    buildings.Any(b => b.ProductionLevel >= 5
                        || b.EfficiencyLevel >= 5
                        || b.CapacityLevel >= 5),

                "Markthaendler" =>
                    // Braucht einen Tracker, kommt spaeter. Erstmal false.
                    false,

                _ => false
            };
        }

        private void ApplyBonus(Farm farm, Achievement achievement)
        {
            switch (achievement.BonusType)
            {
                case "UnlockAllocation":
                    farm.AllocationUnlocked = true;
                    break;
                case "ProductionBoost":
                    farm.AchievementProductionBonus += achievement.BonusValue;
                    break;
                case "SellBoost":
                    farm.AchievementSellBonus += achievement.BonusValue;
                    break;
                case "UpgradeDiscount":
                    farm.AchievementUpgradeDiscount += achievement.BonusValue;
                    break;
                case "StorageBoost":
                    farm.AchievementStorageBonus += achievement.BonusValue;
                    break;
            }
        }
    }
}