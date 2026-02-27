using Microsoft.EntityFrameworkCore;
using SEW04_Projekt_Bsteh.Data;
using SEW04_Projekt_Bsteh.Models;

namespace SEW04_Projekt_Bsteh.Services
{
    // Zentrale Berechnungslogik fuer Idle-Produktion
    // Wird bei jedem Request aufgerufen, berechnet Zeitdifferenz seit letztem Besuch
    public class GameCalculationService
    {
        private readonly ApplicationDbContext _db;

        public GameCalculationService(ApplicationDbContext db)
        {
            _db = db;
        }

        // Hauptmethode: Berechnet alles seit dem letzten Besuch
        public async Task CalculateOfflineProgress(int farmId)
        {
            var farm = await _db.Farms.FindAsync(farmId);
            if (farm == null) return;

            // Zeitdifferenz in Sekunden seit letzter Berechnung
            var now = DateTime.UtcNow;
            var secondsPassed = (now - farm.LastCalculated).TotalSeconds;

            // Weniger als 1 Sekunde vergangen? Nichts tun.
            if (secondsPassed < 1) return;

            // Max 24 Stunden Offline-Progress (Schutz vor Extremwerten)
            secondsPassed = Math.Min(secondsPassed, 86400);

            // Alle Daten laden die wir brauchen
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

            // Produktion pro Gebaeude berechnen, in Reihenfolge der Kette
            // Wichtig: Feld zuerst (kein Input), dann Muehle, dann Baeckerei
            var sortedBuildings = userBuildings
                .OrderBy(ub => ub.Building.InputResourceId == null ? 0 : 1)
                .ThenBy(ub => ub.Building.OutputResourceId)
                .ToList();

            foreach (var ub in sortedBuildings)
            {
                var building = ub.Building;

                // Produktionsrate berechnen (Basis * Upgrades * Rebirth)
                var rate = building.BaseProductionRate;
                rate *= (1 + ub.ProductionLevel * 0.2); // +20% pro Level
                rate *= farm.RebirthMultiplier;

                // Effizienz: weniger Input pro Output
                var efficiency = 1.0 / (1 + ub.EfficiencyLevel * 0.1); // -10% Input pro Level

                // Wie viel koennte produziert werden in der Zeit?
                var maxProduction = rate * secondsPassed;

                // Wenn Input noetig: Pruefen ob genug da ist (ENGPASS!)
                if (building.InputResourceId != null)
                {
                    var inputResource = userResources
                        .FirstOrDefault(ur => ur.ResourceId == building.InputResourceId);

                    if (inputResource == null || inputResource.Amount <= 0)
                    {
                        // Kein Input vorhanden -> Gebaeude steht still
                        continue;
                    }

                    // Wie viel Input wird gebraucht?
                    var inputNeeded = maxProduction * building.InputPerOutput * efficiency;

                    // Wie viel Input ist da? Begrenzt die Produktion
                    // Nur den Teil nehmen der fuer Weiterverarbeitung vorgesehen ist
                    var allocation = allocations
                        .FirstOrDefault(a => a.ResourceId == building.InputResourceId);
                    var processPercent = allocation != null ? (100 - allocation.SellPercentage) / 100.0 : 0;

                    var availableInput = inputResource.Amount * processPercent;

                    if (availableInput <= 0) continue;

                    // Wenn nicht genug Input -> Produktion begrenzen
                    if (inputNeeded > availableInput)
                    {
                        maxProduction = availableInput / (building.InputPerOutput * efficiency);
                        inputNeeded = availableInput;
                    }

                    // Input abziehen
                    inputResource.Amount -= inputNeeded;
                    if (inputResource.Amount < 0) inputResource.Amount = 0;
                }

                // Output zur Ressource hinzufuegen
                var outputResource = userResources
                    .FirstOrDefault(ur => ur.ResourceId == building.OutputResourceId);

                if (outputResource != null)
                {
                    // Kapazitaet beachten (Upgrades erhoehen MaxStorage)
                    var maxStorage = outputResource.MaxStorage * (1 + ub.CapacityLevel * 0.25);
                    outputResource.Amount = Math.Min(outputResource.Amount + maxProduction, maxStorage);
                }
            }

            // Verkauf: Fuer jede Ressource den Verkaufsanteil verkaufen
            decimal totalIncome = 0m;

            foreach (var ur in userResources)
            {
                var allocation = allocations
                    .FirstOrDefault(a => a.ResourceId == ur.ResourceId);
                var sellPercent = allocation != null ? allocation.SellPercentage / 100.0 : 1.0;

                var sellAmount = ur.Amount * sellPercent;
                if (sellAmount <= 0) continue;

                // Geld berechnen
                var income = (decimal)sellAmount * ur.Resource.SellPrice;
                totalIncome += income;

                // Verkaufte Menge abziehen
                ur.Amount -= sellAmount;
                if (ur.Amount < 0) ur.Amount = 0;
            }

            // Geld gutschreiben
            farm.Money += totalIncome;

            // Zeitstempel aktualisieren
            farm.LastCalculated = now;

            await _db.SaveChangesAsync();
        }
    }
}