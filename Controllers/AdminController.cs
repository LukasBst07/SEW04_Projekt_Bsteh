using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEW04_Projekt_Bsteh.Data;
using SEW04_Projekt_Bsteh.Models;

namespace SEW04_Projekt_Bsteh.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Admin Dashboard
        public async Task<IActionResult> Index()
        {
            ViewBag.UserCount = await _userManager.Users.CountAsync();
            ViewBag.FarmCount = await _db.Farms.CountAsync();
            ViewBag.ResourceCount = await _db.Resources.CountAsync();
            ViewBag.BuildingCount = await _db.Buildings.CountAsync();
            ViewBag.AchievementCount = await _db.Achievements.CountAsync();

            return View();
        }

        // === SPIELER VERWALTEN ===
        public async Task<IActionResult> Players(string? search)
        {
            var users = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                users = users.Where(u => u.Email!.ToLower().Contains(search)
                    || u.DisplayName.ToLower().Contains(search));
            }

            var userList = await users.ToListAsync();

            var playerData = new List<PlayerViewModel>();

            foreach (var user in userList)
            {
                var farm = await _db.Farms.FirstOrDefaultAsync(f => f.UserId == user.Id);
                var roles = await _userManager.GetRolesAsync(user);

                playerData.Add(new PlayerViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? "",
                    DisplayName = user.DisplayName,
                    Role = roles.FirstOrDefault() ?? "Spieler",
                    CreatedAt = user.CreatedAt,
                    FarmId = farm?.Id,
                    Money = farm?.Money ?? 0,
                    RebirthCount = farm?.RebirthCount ?? 0,
                    RebirthMultiplier = farm?.RebirthMultiplier ?? 1.0,
                    LastCalculated = farm?.LastCalculated
                });
            }

            ViewBag.Search = search;
            return View(playerData);
        }

        // Spieler bearbeiten (Geld, Multiplikator etc.)
        public async Task<IActionResult> EditPlayer(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var farm = await _db.Farms.FirstOrDefaultAsync(f => f.UserId == userId);

            var userBuildings = farm != null
                ? await _db.UserBuildings.Include(ub => ub.Building)
                    .Where(ub => ub.FarmId == farm.Id).ToListAsync()
                : new List<UserBuilding>();

            var userResources = farm != null
                ? await _db.UserResources.Include(ur => ur.Resource)
                    .Where(ur => ur.FarmId == farm.Id).ToListAsync()
                : new List<UserResource>();

            var userAchievements = farm != null
                ? await _db.UserAchievements.Include(ua => ua.Achievement)
                    .Where(ua => ua.FarmId == farm.Id).ToListAsync()
                : new List<UserAchievement>();

            ViewBag.User = user;
            ViewBag.Farm = farm;
            ViewBag.UserBuildings = userBuildings;
            ViewBag.UserResources = userResources;
            ViewBag.UserAchievements = userAchievements;

            return View();
        }

        // Spieler-Farm bearbeiten (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePlayer(string userId, decimal money,
            double rebirthMultiplier, int rebirthCount, bool allocationUnlocked,
            double productionBonus, double sellBonus, double upgradeDiscount, double storageBonus)
        {
            var farm = await _db.Farms.FirstOrDefaultAsync(f => f.UserId == userId);
            if (farm == null) return NotFound();

            farm.Money = money;
            farm.RebirthMultiplier = rebirthMultiplier;
            farm.RebirthCount = rebirthCount;
            farm.AllocationUnlocked = allocationUnlocked;
            farm.AchievementProductionBonus = productionBonus;
            farm.AchievementSellBonus = sellBonus;
            farm.AchievementUpgradeDiscount = upgradeDiscount;
            farm.AchievementStorageBonus = storageBonus;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Spieler aktualisiert!";
            return RedirectToAction("EditPlayer", new { userId });
        }

        // Spieler-Ressource bearbeiten
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateResource(string userId, int resourceId, double amount, double maxStorage)
        {
            var farm = await _db.Farms.FirstOrDefaultAsync(f => f.UserId == userId);
            if (farm == null) return NotFound();

            var ur = await _db.UserResources
                .FirstOrDefaultAsync(r => r.FarmId == farm.Id && r.ResourceId == resourceId);
            if (ur == null) return NotFound();

            ur.Amount = amount;
            ur.MaxStorage = maxStorage;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Ressource aktualisiert!";
            return RedirectToAction("EditPlayer", new { userId });
        }

        // Spieler-Gebaeude bearbeiten
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBuilding(string userId, int buildingId,
            bool isUnlocked, int productionLevel, int efficiencyLevel, int capacityLevel)
        {
            var farm = await _db.Farms.FirstOrDefaultAsync(f => f.UserId == userId);
            if (farm == null) return NotFound();

            var ub = await _db.UserBuildings
                .FirstOrDefaultAsync(b => b.FarmId == farm.Id && b.BuildingId == buildingId);
            if (ub == null) return NotFound();

            ub.IsUnlocked = isUnlocked;
            ub.ProductionLevel = productionLevel;
            ub.EfficiencyLevel = efficiencyLevel;
            ub.CapacityLevel = capacityLevel;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Gebaeude aktualisiert!";
            return RedirectToAction("EditPlayer", new { userId });
        }

        // Spieler komplett loeschen (Account + Farm + alles)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePlayer(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Eigenen Admin-Account schuetzen
            if (user.Email == "admin@harvestdynasty.com")
            {
                TempData["Error"] = "Admin-Account kann nicht geloescht werden!";
                return RedirectToAction("Players");
            }

            // Farm und alles dazugehoerige loeschen
            var farm = await _db.Farms.FirstOrDefaultAsync(f => f.UserId == userId);
            if (farm != null)
            {
                _db.UserAchievements.RemoveRange(
                    await _db.UserAchievements.Where(ua => ua.FarmId == farm.Id).ToListAsync());
                _db.ResourceAllocations.RemoveRange(
                    await _db.ResourceAllocations.Where(ra => ra.FarmId == farm.Id).ToListAsync());
                _db.UserResources.RemoveRange(
                    await _db.UserResources.Where(ur => ur.FarmId == farm.Id).ToListAsync());
                _db.UserBuildings.RemoveRange(
                    await _db.UserBuildings.Where(ub => ub.FarmId == farm.Id).ToListAsync());
                _db.Farms.Remove(farm);
                await _db.SaveChangesAsync();
            }

            // User loeschen
            await _userManager.DeleteAsync(user);

            TempData["Success"] = $"{user.DisplayName} ({user.Email}) geloescht!";
            return RedirectToAction("Players");
        }

        // === RESSOURCEN VERWALTEN ===
        public async Task<IActionResult> Resources()
        {
            var resources = await _db.Resources.OrderBy(r => r.ChainOrder).ToListAsync();
            return View(resources);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditResource(int id, string name, decimal sellPrice, int chainOrder)
        {
            var resource = await _db.Resources.FindAsync(id);
            if (resource == null) return NotFound();

            resource.Name = name;
            resource.SellPrice = sellPrice;
            resource.ChainOrder = chainOrder;

            await _db.SaveChangesAsync();
            TempData["Success"] = $"{name} aktualisiert!";
            return RedirectToAction("Resources");
        }

        // === GEBAEUDE VERWALTEN ===
        public async Task<IActionResult> Buildings()
        {
            var buildings = await _db.Buildings
                .Include(b => b.InputResource)
                .Include(b => b.OutputResource)
                .ToListAsync();
            return View(buildings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBuilding(int id, string name, string description,
            decimal baseCost, double baseProductionRate, double inputPerOutput)
        {
            var building = await _db.Buildings.FindAsync(id);
            if (building == null) return NotFound();

            building.Name = name;
            building.Description = description;
            building.BaseCost = baseCost;
            building.BaseProductionRate = baseProductionRate;
            building.InputPerOutput = inputPerOutput;

            await _db.SaveChangesAsync();
            TempData["Success"] = $"{name} aktualisiert!";
            return RedirectToAction("Buildings");
        }

        // === ACHIEVEMENTS VERWALTEN ===
        public async Task<IActionResult> Achievements()
        {
            var achievements = await _db.Achievements.ToListAsync();
            return View(achievements);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAchievement(int id, string name, string description,
            string bonusType, double bonusValue, string bonusDescription)
        {
            var achievement = await _db.Achievements.FindAsync(id);
            if (achievement == null) return NotFound();

            achievement.Name = name;
            achievement.Description = description;
            achievement.BonusType = bonusType;
            achievement.BonusValue = bonusValue;
            achievement.BonusDescription = bonusDescription;

            await _db.SaveChangesAsync();
            TempData["Success"] = $"{name} aktualisiert!";
            return RedirectToAction("Achievements");
        }
    }

    // ViewModel fuer Spieler-Uebersicht
    public class PlayerViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int? FarmId { get; set; }
        public decimal Money { get; set; }
        public int RebirthCount { get; set; }
        public double RebirthMultiplier { get; set; }
        public DateTime? LastCalculated { get; set; }
    }
}