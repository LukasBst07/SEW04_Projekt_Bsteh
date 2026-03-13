using Microsoft.EntityFrameworkCore;
using SEW04_Projekt_Bsteh.Data;
using SEW04_Projekt_Bsteh.Models;

namespace SEW04_Projekt_Bsteh.Services
{
    public class GameCalculationService
    {
        private readonly ApplicationDbContext _db;

        public GameCalculationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task CalculateOfflineProgress(int farmId)
        {
            try
            {
                var farm = await _db.Farms.FindAsync(farmId);
                if (farm == null) return;

                var now = DateTime.UtcNow;
                var secondsPassed = (now - farm.LastCalculated).TotalSeconds;

                if (secondsPassed < 1) return;

                // Max 24h Offline-Progress
                secondsPassed = Math.Min(secondsPassed, 86400);

                var userBuildings = await _db.UserBuildings
                    .Include(ub => ub.Building)
                    .Where(ub => ub.FarmId == farmId && ub.IsUnlocked)
                    .ToListAsync();

                var userResources = await _db.UserResources
                    .Include(ur => ur.Resource)
                    .Where(ur => ur.FarmId == farmId)
                    .ToListAsync();

                var allocations = await _db.ResourceAllocations
                    .Where(ra => ra.FarmId == farmId)
                    .ToListAsync();

                // Sortierung: Feld zuerst, dann Muehle, dann Baeckerei
                var sortedBuildings = userBuildings
                    .OrderBy(ub => ub.Building.InputResourceId == null ? 0 : 1)
                    .ThenBy(ub => ub.Building.OutputResourceId)
                    .ToList();

                foreach (var ub in sortedBuildings)
                {
                    var building = ub.Building;

                    // Produktionsrate mit allen Boni
                    var rate = building.BaseProductionRate;
                    rate *= (1 + ub.ProductionLevel * 0.2);
                    rate *= farm.RebirthMultiplier;
                    rate *= (1 + farm.AchievementProductionBonus);

                    var efficiency = 1.0 / (1 + ub.EfficiencyLevel * 0.1);

                    var maxProduction = rate * secondsPassed;

                    if (building.InputResourceId != null)
                    {
                        var inputResource = userResources
                            .FirstOrDefault(ur => ur.ResourceId == building.InputResourceId);

                        if (inputResource == null || inputResource.Amount <= 0)
                            continue;

                        var inputNeeded = maxProduction * building.InputPerOutput * efficiency;

                        // Nur weiterverarbeiten wenn Allocation freigeschaltet
                        double processPercent;
                        if (farm.AllocationUnlocked)
                        {
                            var allocation = allocations
                                .FirstOrDefault(a => a.ResourceId == building.InputResourceId);
                            processPercent = allocation != null ? (100 - allocation.SellPercentage) / 100.0 : 0;
                        }
                        else
                        {
                            processPercent = 0;
                        }

                        var availableInput = inputResource.Amount * processPercent;

                        if (availableInput <= 0) continue;

                        if (inputNeeded > availableInput)
                        {
                            maxProduction = availableInput / (building.InputPerOutput * efficiency);
                            inputNeeded = availableInput;
                        }

                        inputResource.Amount -= inputNeeded;
                        if (inputResource.Amount < 0) inputResource.Amount = 0;
                    }

                    var outputResource = userResources
                        .FirstOrDefault(ur => ur.ResourceId == building.OutputResourceId);

                    if (outputResource != null)
                    {
                        // BUG FIX: Korrekte MaxStorage Berechnung
                        var effectiveMaxStorage = CalculateEffectiveMaxStorage(outputResource.MaxStorage, farm);
                        var newAmount = outputResource.Amount + maxProduction;
                        // BUG FIX: Lager darf nicht ueberschritten werden
                        outputResource.Amount = Math.Min(newAmount, effectiveMaxStorage);
                    }
                }

                // Verkauf mit Achievement-Preisbonus
                decimal totalIncome = 0m;

                foreach (var ur in userResources)
                {
                    double sellPercent;
                    if (farm.AllocationUnlocked)
                    {
                        var allocation = allocations
                            .FirstOrDefault(a => a.ResourceId == ur.ResourceId);
                        sellPercent = allocation != null ? allocation.SellPercentage / 100.0 : 1.0;

                        // BUG FIX: Letzte Ressource in der Kette (hoechster ChainOrder)
                        // hat keine Weiterverarbeitung, immer 100% verkaufen wenn nicht anders eingestellt
                    }
                    else
                    {
                        sellPercent = 1.0;
                    }

                    var sellAmount = ur.Amount * sellPercent;
                    if (sellAmount <= 0) continue;

                    // Preis mit Sell-Bonus
                    var price = ur.Resource.SellPrice * (1 + (decimal)farm.AchievementSellBonus);
                    var income = (decimal)sellAmount * price;
                    totalIncome += income;

                    ur.Amount -= sellAmount;
                    if (ur.Amount < 0) ur.Amount = 0;
                }

                farm.Money += totalIncome;
                farm.LastCalculated = now;

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Fehler loggen aber App nicht crashen lassen
                Console.WriteLine($"Fehler bei Idle-Berechnung: {ex.Message}");
            }
        }

        // Effektive MaxStorage berechnen mit allen Boni
        public static double CalculateEffectiveMaxStorage(double baseMaxStorage, Farm farm)
        {
            return baseMaxStorage * (1 + farm.AchievementStorageBonus);
        }
    }
}