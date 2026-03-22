using SEW04_Projekt_Bsteh.Models;

namespace SEW04_Projekt_Bsteh.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext db)
        {
            if (db.Resources.Any()) return;

            var weizen = new Resource { Name = "Weizen", SellPrice = 1m, ChainOrder = 0 };
            var mehl = new Resource { Name = "Mehl", SellPrice = 4m, ChainOrder = 1 };
            var brot = new Resource { Name = "Brot", SellPrice = 15m, ChainOrder = 2 };

            db.Resources.AddRange(weizen, mehl, brot);
            db.SaveChanges();

            var feld = new Building
            {
                Name = "Feld",
                Description = "Produziert Weizen aus dem Nichts.",
                BaseCost = 0m,
                BaseProductionRate = 0.5,
                InputResourceId = null,
                OutputResourceId = weizen.Id,
                InputPerOutput = 0
            };

            var muehle = new Building
            {
                Name = "Mühle",
                Description = "Verarbeitet Weizen zu Mehl.",
                BaseCost = 800m,
                BaseProductionRate = 0.2,
                InputResourceId = weizen.Id,
                OutputResourceId = mehl.Id,
                InputPerOutput = 3.0
            };

            var baeckerei = new Building
            {
                Name = "Bäckerei",
                Description = "Verarbeitet Mehl zu Brot.",
                BaseCost = 5000m,
                BaseProductionRate = 0.08,
                InputResourceId = mehl.Id,
                OutputResourceId = brot.Id,
                InputPerOutput = 4.0
            };

            db.Buildings.AddRange(feld, muehle, baeckerei);
            db.SaveChanges();

            db.Achievements.AddRange(
                new Achievement
                {
                    Name = "Mühlenbesitzer",
                    Description = "Kaufe die Mühle.",
                    BonusType = "UnlockAllocation",
                    BonusValue = 0,
                    BonusDescription = "Schaltet die Ressourcenverteilung frei."
                },
                new Achievement
                {
                    Name = "Bäckermeister",
                    Description = "Kaufe die Bäckerei.",
                    BonusType = "StorageBoost",
                    BonusValue = 0.5,
                    BonusDescription = "Lagerkapazität aller Ressourcen +50%"
                },
                new Achievement
                {
                    Name = "Erste 1000 Münzen",
                    Description = "Erreiche 1000 Münzen.",
                    BonusType = "ProductionBoost",
                    BonusValue = 0.1,
                    BonusDescription = "Produktionsrate aller Gebäude +10%"
                },
                new Achievement
                {
                    Name = "Vollständige Kette",
                    Description = "Schalte alle 3 Gebäude frei.",
                    BonusType = "UpgradeDiscount",
                    BonusValue = 0.15,
                    BonusDescription = "Upgrade-Kosten -15%"
                },
                new Achievement
                {
                    Name = "Lagermeister",
                    Description = "Fülle ein Lager komplett.",
                    BonusType = "StorageBoost",
                    BonusValue = 1.0,
                    BonusDescription = "Lagerkapazität aller Ressourcen nochmal +100%"
                },
                new Achievement
                {
                    Name = "Erste 10000 Münzen",
                    Description = "Erreiche 10000 Münzen.",
                    BonusType = "SellBoost",
                    BonusValue = 0.2,
                    BonusDescription = "Verkaufspreise +20%"
                },
                new Achievement
                {
                    Name = "Upgrade-Anfänger",
                    Description = "Bringe ein Upgrade auf Level 5.",
                    BonusType = "ProductionBoost",
                    BonusValue = 0.15,
                    BonusDescription = "Produktionsrate aller Gebäude +15%"
                },
                new Achievement
                {
                    Name = "Markthändler",
                    Description = "Verkaufe manuell 100 Einheiten am Marktplatz.",
                    BonusType = "SellBoost",
                    BonusValue = 0.1,
                    BonusDescription = "Verkaufspreise +10%"
                }
            );
            db.SaveChanges();
        }
    }
}