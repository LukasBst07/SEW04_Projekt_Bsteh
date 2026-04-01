using Microsoft.EntityFrameworkCore;
using SEW04_Projekt_Bsteh.Data;
using SEW04_Projekt_Bsteh.Models;

namespace SEW04_Projekt_Bsteh.Services
{
    public class AchievementService
    {
        private readonly ApplicationDbContext _db;

        public AchievementService(ApplicationDbContext db)
        {
            _db = db;
        }

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
                if (unlockedIds.Contains(achievement.Id)) continue;

                bool earned = CheckCondition(achievement, farm, userBuildings, userResources);

                if (earned)
                {
                    _db.UserAchievements.Add(new UserAchievement
                    {
                        FarmId = farmId,
                        AchievementId = achievement.Id,
                        UnlockedAt = DateTime.UtcNow
                    });

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
                "Mühlenbesitzer" =>
                    buildings.Any(b => b.Building.Name == "Mühle" && b.IsUnlocked),

                "Bäckermeister" =>
                    buildings.Any(b => b.Building.Name == "Bäckerei" && b.IsUnlocked),

                "Vollständige Kette" =>
                    buildings.Any(b => b.ProductionLevel >= 3
                        || b.EfficiencyLevel >= 3
                        || b.CapacityLevel >= 3),

                "Erste 1000 Münzen" =>
                    farm.Money >= 1000,

                "Sparfuchs" =>
                    farm.Money >= 10000,

                "Lagermeister" =>
                    resources.Any(r => r.Amount >= r.MaxStorage * 0.99),

                "Upgrade-Anfänger" =>
                    buildings.Any(b => b.ProductionLevel >= 5
                        || b.EfficiencyLevel >= 5
                        || b.CapacityLevel >= 5),

                "Markthändler" =>
                    farm.ManualSellTotal >= 100,

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