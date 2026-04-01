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

        public async Task<IActionResult> Index()
        {
            ViewBag.UserCount = await _userManager.Users.CountAsync();
            ViewBag.FarmCount = await _db.Farms.CountAsync();

            return View();
        }

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

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                playerData = playerData.Where(p =>
                    p.Email.ToLower().Contains(search) ||
                    p.DisplayName.ToLower().Contains(search)).ToList();
            }

            if (!string.IsNullOrEmpty(role) && role != "all")
            {
                playerData = playerData.Where(p => p.Role == role).ToList();
            }

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

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string displayName, string email, string password, string role)
        {
            if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Alle Felder müssen ausgefüllt sein!";
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = displayName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                if (role == "Admin" || role == "Spieler")
                    await _userManager.AddToRoleAsync(user, role);
                else
                    await _userManager.AddToRoleAsync(user, "Spieler");

                TempData["Success"] = $"{displayName} ({email}) als {role} erstellt!";
                return RedirectToAction("Players");
            }

            foreach (var error in result.Errors)
            {
                TempData["Error"] = error.Description;
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AjaxChangeRole([FromBody] ChangeRoleRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null) return BadRequest("User nicht gefunden.");

                if (user.Email == "admin@harvestdynasty.com")
                    return BadRequest("Haupt-Admin kann nicht geändert werden!");

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, request.NewRole);

                return Json(new { success = true, message = $"{user.DisplayName} ist jetzt {request.NewRole}!" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Fehler: {ex.Message}");
            }
        }

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
                if (ub == null) return BadRequest("Gebäude nicht gefunden.");

                ub.IsUnlocked = request.IsUnlocked;
                ub.ProductionLevel = request.ProductionLevel;
                ub.EfficiencyLevel = request.EfficiencyLevel;
                ub.CapacityLevel = request.CapacityLevel;

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Gebäude aktualisiert!" });
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

                farm.Money = 100m;
                farm.RebirthMultiplier = 1.0;
                farm.RebirthCount = 0;
                farm.AllocationUnlocked = false;
                farm.AchievementProductionBonus = 0;
                farm.AchievementSellBonus = 0;
                farm.AchievementUpgradeDiscount = 0;
                farm.AchievementStorageBonus = 0;
                farm.ManualSellTotal = 0;
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
                return Json(new { success = true, message = "Spieler komplett zurückgesetzt!" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Fehler: {ex.Message}");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePlayer(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (user.Email == "admin@harvestdynasty.com")
            {
                TempData["Error"] = "Admin-Account kann nicht gelöscht werden!";
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

            TempData["Success"] = $"{user.DisplayName} ({user.Email}) gelöscht!";
            return RedirectToAction("Players");
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

    public class ChangeRoleRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string NewRole { get; set; } = string.Empty;
    }
}