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

                // Zuerst: MaxStorage in DB synchronisieren
                foreach (var ub in userBuildings)
                {
                    var outputRes = userResources
                        .FirstOrDefault(ur => ur.ResourceId == ub.Building.OutputResourceId);
                    if (outputRes != null)
                    {
                        var correctMax = 100.0 * (1 + ub.CapacityLevel * 0.20) * (1 + farm.AchievementStorageBonus);
                        outputRes.MaxStorage = correctMax;
                    }
                }

                // Sortierung: Feld zuerst, dann Mühle, dann Bäckerei
                var sortedBuildings = userBuildings
                    .OrderBy(ub => ub.Building.InputResourceId == null ? 0 : 1)
                    .ThenBy(ub => ub.Building.OutputResourceId)
                    .ToList();

                foreach (var ub in sortedBuildings)
                {
                    var building = ub.Building;

                    var rate = building.BaseProductionRate;
                    rate *= (1 + ub.ProductionLevel * 0.10);
                    rate *= farm.RebirthMultiplier;
                    rate *= (1 + farm.AchievementProductionBonus);

                    var efficiency = 1.0 / (1 + ub.EfficiencyLevel * 0.08);

                    var maxProduction = rate * secondsPassed;

                    if (building.InputResourceId != null)
                    {
                        var inputResource = userResources
                            .FirstOrDefault(ur => ur.ResourceId == building.InputResourceId);

                        if (inputResource == null || inputResource.Amount <= 0)
                            continue;

                        var inputNeeded = maxProduction * building.InputPerOutput * efficiency;

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
                        var newAmount = outputResource.Amount + maxProduction;
                        // Lager HART begrenzen
                        if (newAmount > outputResource.MaxStorage)
                            newAmount = outputResource.MaxStorage;
                        outputResource.Amount = newAmount;
                    }
                }

                // Auto-Verkauf: Batched statt alles auf einmal
                // Verkauft maximal (rate * seconds) pro Tick, nicht den ganzen Bestand
                decimal totalIncome = 0m;

                foreach (var ur in userResources)
                {
                    double sellPercent;
                    if (farm.AllocationUnlocked)
                    {
                        var allocation = allocations
                            .FirstOrDefault(a => a.ResourceId == ur.ResourceId);
                        sellPercent = allocation != null ? allocation.SellPercentage / 100.0 : 1.0;
                    }
                    else
                    {
                        sellPercent = 1.0;
                    }

                    if (sellPercent <= 0) continue;

                    // Batch-Verkauf: Max 10 Einheiten pro 5 Sekunden = 2/s
                    var maxSellPerSecond = 2.0;
                    var maxSellThisTick = maxSellPerSecond * secondsPassed;

                    var wantToSell = ur.Amount * sellPercent;
                    var actualSell = Math.Min(wantToSell, maxSellThisTick);

                    if (actualSell <= 0) continue;

                    var price = ur.Resource.SellPrice * (1 + (decimal)farm.AchievementSellBonus);
                    var income = (decimal)actualSell * price;
                    totalIncome += income;

                    ur.Amount -= actualSell;
                    if (ur.Amount < 0) ur.Amount = 0;
                }

                // Nochmal sicherstellen: Kein Lager über Maximum
                foreach (var ur in userResources)
                {
                    if (ur.Amount > ur.MaxStorage)
                        ur.Amount = ur.MaxStorage;
                }

                farm.Money += totalIncome;
                farm.LastCalculated = now;

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler bei Idle-Berechnung: {ex.Message}");
            }
        }
    }
}