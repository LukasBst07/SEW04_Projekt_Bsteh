using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEW04_Projekt_Bsteh.Data;
using SEW04_Projekt_Bsteh.Models;
using SEW04_Projekt_Bsteh.Services;

namespace SEW04_Projekt_Bsteh.Controllers
{
    [Authorize]
    public class GameController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly GameCalculationService _calc;
        private readonly AchievementService _achievements;

        public GameController(ApplicationDbContext db, UserManager<IdentityUser> userManager,
            GameCalculationService calc, AchievementService achievements)
        {
            _db = db;
            _userManager = userManager;
            _calc = calc;
            _achievements = achievements;
        }

        // Farm laden, bei Bedarf erstellen, Idle berechnen, Achievements pruefen
        private async Task<Farm?> LoadFarmWithCalculation()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return null;

            var farm = await _db.Farms.FirstOrDefaultAsync(f => f.UserId == userId);

            if (farm == null)
            {
                farm = await CreateNewFarm(userId);
            }

            await _calc.CalculateOfflineProgress(farm.Id);
            await _achievements.CheckAchievements(farm.Id);

            return await _db.Farms.FirstOrDefaultAsync(f => f.UserId == userId);
        }

        // ==================== DASHBOARD ====================
        public async Task<IActionResult> Dashboard()
        {
            var farm = await LoadFarmWithCalculation();
            if (farm == null) return RedirectToAction("Index", "Home");

            var userBuildings = await _db.UserBuildings
                .Include(ub => ub.Building)
                .Where(ub => ub.FarmId == farm.Id)
                .ToListAsync();

            var userResources = await _db.UserResources
                .Include(ur => ur.Resource)
                .Where(ur => ur.FarmId == farm.Id)
                .OrderBy(ur => ur.Resource.ChainOrder)
                .ToListAsync();

            ViewBag.Farm = farm;
            ViewBag.UserBuildings = userBuildings;
            ViewBag.UserResources = userResources;

            return View();
        }

        // ==================== GEBAEUDE ====================
        public async Task<IActionResult> Buildings()
        {
            var farm = await LoadFarmWithCalculation();
            if (farm == null) return RedirectToAction("Index", "Home");

            var userBuildings = await _db.UserBuildings
                .Include(ub => ub.Building)
                    .ThenInclude(b => b.OutputResource)
                .Include(ub => ub.Building)
                    .ThenInclude(b => b.InputResource)
                .Where(ub => ub.FarmId == farm.Id)
                .ToListAsync();

            ViewBag.Farm = farm;
            ViewBag.UserBuildings = userBuildings;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyBuilding(int buildingId)
        {
            var farm = await LoadFarmWithCalculation();
            if (farm == null) return RedirectToAction("Buildings");

            var userBuilding = await _db.UserBuildings
                .Include(ub => ub.Building)
                .FirstOrDefaultAsync(ub => ub.FarmId == farm.Id && ub.BuildingId == buildingId);

            if (userBuilding == null) return RedirectToAction("Buildings");

            if (userBuilding.IsUnlocked)
            {
                TempData["Error"] = "Gebaeude ist bereits freigeschaltet.";
                return RedirectToAction("Buildings");
            }

            if (farm.Money < userBuilding.Building.BaseCost)
            {
                TempData["Error"] = "Nicht genug Geld!";
                return RedirectToAction("Buildings");
            }

            farm.Money -= userBuilding.Building.BaseCost;
            userBuilding.IsUnlocked = true;

            await _db.SaveChangesAsync();

            // Achievements nochmal pruefen nach Kauf
            await _achievements.CheckAchievements(farm.Id);

            TempData["Success"] = $"{userBuilding.Building.Name} gekauft!";
            return RedirectToAction("Buildings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpgradeBuilding(int buildingId, string upgradeType)
        {
            var farm = await LoadFarmWithCalculation();
            if (farm == null) return RedirectToAction("Buildings");

            var userBuilding = await _db.UserBuildings
                .Include(ub => ub.Building)
                .FirstOrDefaultAsync(ub => ub.FarmId == farm.Id && ub.BuildingId == buildingId);

            if (userBuilding == null || !userBuilding.IsUnlocked)
                return RedirectToAction("Buildings");

            int currentLevel = upgradeType switch
            {
                "production" => userBuilding.ProductionLevel,
                "efficiency" => userBuilding.EfficiencyLevel,
                "capacity" => userBuilding.CapacityLevel,
                _ => -1
            };

            if (currentLevel == -1) return RedirectToAction("Buildings");

            var cost = userBuilding.Building.BaseCost * (decimal)(currentLevel + 1) * 0.5m;
            if (cost < 50) cost = 50;

            // Achievement-Rabatt anwenden
            cost *= (1 - (decimal)farm.AchievementUpgradeDiscount);

            if (farm.Money < cost)
            {
                TempData["Error"] = $"Nicht genug Geld! Kosten: {cost:F0}";
                return RedirectToAction("Buildings");
            }

            farm.Money -= cost;

            switch (upgradeType)
            {
                case "production":
                    userBuilding.ProductionLevel++;
                    break;
                case "efficiency":
                    userBuilding.EfficiencyLevel++;
                    break;
                case "capacity":
                    userBuilding.CapacityLevel++;
                    var outputResource = await _db.UserResources
                        .FirstOrDefaultAsync(ur => ur.FarmId == farm.Id && ur.ResourceId == userBuilding.Building.OutputResourceId);
                    if (outputResource != null)
                    {
                        outputResource.MaxStorage = 100 * (1 + userBuilding.CapacityLevel * 0.25)
                            * (1 + farm.AchievementStorageBonus);
                    }
                    break;
            }

            await _db.SaveChangesAsync();

            // Achievements nochmal pruefen nach Upgrade
            await _achievements.CheckAchievements(farm.Id);

            TempData["Success"] = $"{userBuilding.Building.Name} - {upgradeType} auf Level {currentLevel + 1} verbessert!";
            return RedirectToAction("Buildings");
        }

        // ==================== RESSOURCEN ====================
        public async Task<IActionResult> Resources()
        {
            var farm = await LoadFarmWithCalculation();
            if (farm == null) return RedirectToAction("Index", "Home");

            var userResources = await _db.UserResources
                .Include(ur => ur.Resource)
                .Where(ur => ur.FarmId == farm.Id)
                .OrderBy(ur => ur.Resource.ChainOrder)
                .ToListAsync();

            var allocations = await _db.ResourceAllocations
                .Include(ra => ra.Resource)
                .Where(ra => ra.FarmId == farm.Id)
                .ToListAsync();

            ViewBag.Farm = farm;
            ViewBag.UserResources = userResources;
            ViewBag.Allocations = allocations;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAllocation(int resourceId, int sellPercentage)
        {
            var farm = await LoadFarmWithCalculation();
            if (farm == null) return RedirectToAction("Resources");

            // Nur wenn freigeschaltet
            if (!farm.AllocationUnlocked)
            {
                TempData["Error"] = "Ressourcenverteilung ist noch gesperrt!";
                return RedirectToAction("Resources");
            }

            sellPercentage = Math.Clamp(sellPercentage, 0, 100);

            var allocation = await _db.ResourceAllocations
                .FirstOrDefaultAsync(ra => ra.FarmId == farm.Id && ra.ResourceId == resourceId);

            if (allocation == null) return RedirectToAction("Resources");

            allocation.SellPercentage = sellPercentage;
            await _db.SaveChangesAsync();

            return RedirectToAction("Resources");
        }

        // ==================== ACHIEVEMENTS ====================
        public async Task<IActionResult> Achievements()
        {
            var farm = await LoadFarmWithCalculation();
            if (farm == null) return RedirectToAction("Index", "Home");

            var userAchievements = await _db.UserAchievements
                .Where(ua => ua.FarmId == farm.Id)
                .Select(ua => ua.AchievementId)
                .ToListAsync();

            var allAchievements = await _db.Achievements.ToListAsync();

            ViewBag.Farm = farm;
            ViewBag.UserAchievements = userAchievements;
            ViewBag.AllAchievements = allAchievements;

            return View();
        }

        // ==================== MARKTPLATZ ====================
        public async Task<IActionResult> Market()
        {
            var farm = await LoadFarmWithCalculation();
            if (farm == null) return RedirectToAction("Index", "Home");

            var userResources = await _db.UserResources
                .Include(ur => ur.Resource)
                .Where(ur => ur.FarmId == farm.Id)
                .OrderBy(ur => ur.Resource.ChainOrder)
                .ToListAsync();

            ViewBag.Farm = farm;
            ViewBag.UserResources = userResources;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SellResource(int resourceId, double amount)
        {
            var farm = await LoadFarmWithCalculation();
            if (farm == null) return RedirectToAction("Market");

            var userResource = await _db.UserResources
                .Include(ur => ur.Resource)
                .FirstOrDefaultAsync(ur => ur.FarmId == farm.Id && ur.ResourceId == resourceId);

            if (userResource == null) return RedirectToAction("Market");

            amount = Math.Min(amount, userResource.Amount);
            if (amount <= 0)
            {
                TempData["Error"] = "Nichts zu verkaufen!";
                return RedirectToAction("Market");
            }

            // Preis mit Achievement-Bonus
            var price = userResource.Resource.SellPrice * (1 + (decimal)farm.AchievementSellBonus);
            var income = (decimal)amount * price;
            userResource.Amount -= amount;
            farm.Money += income;

            await _db.SaveChangesAsync();

            // Achievements pruefen nach Verkauf
            await _achievements.CheckAchievements(farm.Id);

            TempData["Success"] = $"{amount:F1} {userResource.Resource.Name} fuer {income:F2} verkauft!";
            return RedirectToAction("Market");
        }

        // ==================== PROFIL ====================
        public async Task<IActionResult> Profile()
        {
            var farm = await LoadFarmWithCalculation();
            if (farm == null) return RedirectToAction("Index", "Home");

            var userBuildings = await _db.UserBuildings
                .Include(ub => ub.Building)
                .Where(ub => ub.FarmId == farm.Id)
                .ToListAsync();

            var userAchievements = await _db.UserAchievements
                .Where(ua => ua.FarmId == farm.Id)
                .ToListAsync();

            var user = await _userManager.GetUserAsync(User);

            ViewBag.Farm = farm;
            ViewBag.UserBuildings = userBuildings;
            ViewBag.AchievementCount = userAchievements.Count;
            ViewBag.TotalAchievements = await _db.Achievements.CountAsync();
            ViewBag.UserEmail = user?.Email ?? "Unbekannt";

            return View();
        }

        // ==================== FARM ERSTELLEN ====================
        private async Task<Farm> CreateNewFarm(string userId)
        {
            var farm = new Farm
            {
                UserId = userId,
                Money = 100m,
                RebirthMultiplier = 1.0,
                LastCalculated = DateTime.UtcNow
            };
            _db.Farms.Add(farm);
            await _db.SaveChangesAsync();

            var buildings = await _db.Buildings.ToListAsync();
            foreach (var building in buildings)
            {
                _db.UserBuildings.Add(new UserBuilding
                {
                    FarmId = farm.Id,
                    BuildingId = building.Id,
                    IsUnlocked = building.Name == "Feld",
                    ProductionLevel = 0,
                    EfficiencyLevel = 0,
                    CapacityLevel = 0
                });
            }

            var resources = await _db.Resources.ToListAsync();
            foreach (var resource in resources)
            {
                _db.UserResources.Add(new UserResource
                {
                    FarmId = farm.Id,
                    ResourceId = resource.Id,
                    Amount = 0,
                    MaxStorage = 100
                });

                _db.ResourceAllocations.Add(new ResourceAllocation
                {
                    FarmId = farm.Id,
                    ResourceId = resource.Id,
                    SellPercentage = 100
                });
            }

            await _db.SaveChangesAsync();
            return farm;
        }
    }
}