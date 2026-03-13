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
        public async Task<IActionResult> Players(string? search, string? role, string? sortBy)
        {
            var users = await _userManager.Users.ToListAsync();
            var playerData = new List<PlayerViewModel>();

            foreach (var user in users)
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

            // Suche
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                playerData = playerData.Where(p =>
                    p.Email.ToLower().Contains(search) ||
                    p.DisplayName.ToLower().Contains(search)).ToList();
            }

            // Rollenfilter
            if (!string.IsNullOrEmpty(role) && role != "all")
            {
                playerData = playerData.Where(p => p.Role == role).ToList();
            }

            // Sortierung
            playerData = sortBy switch
            {
                "money_desc" => playerData.OrderByDescending(p => p.Money).ToList(),
                "money_asc" => playerData.OrderBy(p => p.Money).ToList(),
                "login_desc" => playerData.OrderByDescending(p => p.LastCalculated).ToList(),
                "login_asc" => playerData.OrderBy(p => p.LastCalculated).ToList(),
                "name" => playerData.OrderBy(p => p.DisplayName).ToList(),
                _ => playerData.OrderBy(p => p.DisplayName).ToList()
            };

            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.SortBy = sortBy;

            return View(playerData);
        }

        // Spieler bearbeiten
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

        // === AJAX ENDPOINTS ===

        [HttpPost]
        public async Task<IActionResult> AjaxUpdatePlayer([FromBody] UpdatePlayerRequest request)
        {
            try
            {
                var farm = await _db.Farms.FirstOrDefaultAsync(f => f.UserId == request.UserId);
                if (farm == null) return BadRequest("Farm nicht gefunden.");

                farm.Money = request.Money;
                farm.RebirthMultiplier = request.RebirthMultiplier;
                farm.RebirthCount = request.RebirthCount;
                farm.AllocationUnlocked = request.AllocationUnlocked;
                farm.AchievementProductionBonus = request.ProductionBonus;
                farm.AchievementSellBonus = request.SellBonus;
                farm.AchievementUpgradeDiscount = request.UpgradeDiscount;
                farm.AchievementStorageBonus = request.StorageBonus;

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Farm aktualisiert!" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Fehler: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AjaxUpdateBuilding([FromBody] UpdateBuildingRequest request)
        {
            try
            {
                var farm = await _db.Farms.FirstOrDefaultAsync(f => f.UserId == request.UserId);
                if (farm == null) return BadRequest("Farm nicht gefunden.");

                var ub = await _db.UserBuildings
                    .FirstOrDefaultAsync(b => b.FarmId == farm.Id && b.BuildingId == request.BuildingId);
                if (ub == null) return BadRequest("Gebaeude nicht gefunden.");

                ub.IsUnlocked = request.IsUnlocked;
                ub.ProductionLevel = request.ProductionLevel;
                ub.EfficiencyLevel = request.EfficiencyLevel;
                ub.CapacityLevel = request.CapacityLevel;

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Gebaeude aktualisiert!" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Fehler: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AjaxUpdateResource([FromBody] UpdateResourceRequest request)
        {
            try
            {
                var farm = await _db.Farms.FirstOrDefaultAsync(f => f.UserId == request.UserId);
                if (farm == null) return BadRequest("Farm nicht gefunden.");

                var ur = await _db.UserResources
                    .FirstOrDefaultAsync(r => r.FarmId == farm.Id && r.ResourceId == request.ResourceId);
                if (ur == null) return BadRequest("Ressource nicht gefunden.");

                ur.Amount = request.Amount;
                ur.MaxStorage = request.MaxStorage;

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Ressource aktualisiert!" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Fehler: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AjaxResetPlayer([FromBody] string userId)
        {
            try
            {
                var farm = await _db.Farms.FirstOrDefaultAsync(f => f.UserId == userId);
                if (farm == null) return BadRequest("Farm nicht gefunden.");

                // Komplett-Reset: Alles auf Anfang
                farm.Money = 100m;
                farm.RebirthMultiplier = 1.0;
                farm.RebirthCount = 0;
                farm.AllocationUnlocked = false;
                farm.AchievementProductionBonus = 0;
                farm.AchievementSellBonus = 0;
                farm.AchievementUpgradeDiscount = 0;
                farm.AchievementStorageBonus = 0;
                farm.LastCalculated = DateTime.UtcNow;

                var userBuildings = await _db.UserBuildings
                    .Include(ub => ub.Building)
                    .Where(ub => ub.FarmId == farm.Id)
                    .ToListAsync();

                foreach (var ub in userBuildings)
                {
                    ub.IsUnlocked = ub.Building.Name == "Feld";
                    ub.ProductionLevel = 0;
                    ub.EfficiencyLevel = 0;
                    ub.CapacityLevel = 0;
                }

                var userResources = await _db.UserResources
                    .Where(ur => ur.FarmId == farm.Id).ToListAsync();

                foreach (var ur in userResources)
                {
                    ur.Amount = 0;
                    ur.MaxStorage = 100;
                }

                var allocs = await _db.ResourceAllocations
                    .Where(ra => ra.FarmId == farm.Id).ToListAsync();

                foreach (var a in allocs)
                {
                    a.SellPercentage = 100;
                }

                var achievements = await _db.UserAchievements
                    .Where(ua => ua.FarmId == farm.Id).ToListAsync();
                _db.UserAchievements.RemoveRange(achievements);

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Spieler komplett zurueckgesetzt!" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Fehler: {ex.Message}");
            }
        }

        // Spieler loeschen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePlayer(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (user.Email == "admin@harvestdynasty.com")
            {
                TempData["Error"] = "Admin-Account kann nicht geloescht werden!";
                return RedirectToAction("Players");
            }

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
        public async Task<IActionResult> AjaxEditResource([FromBody] EditResourceRequest request)
        {
            try
            {
                var resource = await _db.Resources.FindAsync(request.Id);
                if (resource == null) return BadRequest("Ressource nicht gefunden.");

                resource.Name = request.Name;
                resource.SellPrice = request.SellPrice;
                resource.ChainOrder = request.ChainOrder;

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = $"{request.Name} aktualisiert!" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Fehler: {ex.Message}");
            }
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
        public async Task<IActionResult> AjaxEditBuilding([FromBody] EditBuildingRequest request)
        {
            try
            {
                var building = await _db.Buildings.FindAsync(request.Id);
                if (building == null) return BadRequest("Gebaeude nicht gefunden.");

                building.Name = request.Name;
                building.Description = request.Description;
                building.BaseCost = request.BaseCost;
                building.BaseProductionRate = request.BaseProductionRate;
                building.InputPerOutput = request.InputPerOutput;

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = $"{request.Name} aktualisiert!" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Fehler: {ex.Message}");
            }
        }

        // === ACHIEVEMENTS VERWALTEN ===
        public async Task<IActionResult> Achievements()
        {
            var achievements = await _db.Achievements.ToListAsync();
            return View(achievements);
        }

        [HttpPost]
        public async Task<IActionResult> AjaxEditAchievement([FromBody] EditAchievementRequest request)
        {
            try
            {
                var achievement = await _db.Achievements.FindAsync(request.Id);
                if (achievement == null) return BadRequest("Achievement nicht gefunden.");

                achievement.Name = request.Name;
                achievement.Description = request.Description;
                achievement.BonusType = request.BonusType;
                achievement.BonusValue = request.BonusValue;
                achievement.BonusDescription = request.BonusDescription;

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = $"{request.Name} aktualisiert!" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Fehler: {ex.Message}");
            }
        }
    }

    // === VIEW MODELS & REQUEST MODELS ===

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

    public class UpdatePlayerRequest
    {
        public string UserId { get; set; } = string.Empty;
        public decimal Money { get; set; }
        public double RebirthMultiplier { get; set; }
        public int RebirthCount { get; set; }
        public bool AllocationUnlocked { get; set; }
        public double ProductionBonus { get; set; }
        public double SellBonus { get; set; }
        public double UpgradeDiscount { get; set; }
        public double StorageBonus { get; set; }
    }

    public class UpdateBuildingRequest
    {
        public string UserId { get; set; } = string.Empty;
        public int BuildingId { get; set; }
        public bool IsUnlocked { get; set; }
        public int ProductionLevel { get; set; }
        public int EfficiencyLevel { get; set; }
        public int CapacityLevel { get; set; }
    }

    public class UpdateResourceRequest
    {
        public string UserId { get; set; } = string.Empty;
        public int ResourceId { get; set; }
        public double Amount { get; set; }
        public double MaxStorage { get; set; }
    }

    public class EditResourceRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal SellPrice { get; set; }
        public int ChainOrder { get; set; }
    }

    public class EditBuildingRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BaseCost { get; set; }
        public double BaseProductionRate { get; set; }
        public double InputPerOutput { get; set; }
    }

    public class EditAchievementRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BonusType { get; set; } = string.Empty;
        public double BonusValue { get; set; }
        public string BonusDescription { get; set; } = string.Empty;
    }
}