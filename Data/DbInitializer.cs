using SEW04_Projekt_Bsteh.Models;

namespace SEW04_Projekt_Bsteh.Data
{
    // Befuellt die Datenbank mit Stammdaten beim ersten Start
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext db)
        {
            // Wenn schon Ressourcen da sind, nicht nochmal seeden
            if (db.Resources.Any()) return;

            // === RESSOURCEN ===
            var weizen = new Resource { Name = "Weizen", SellPrice = 2m, ChainOrder = 0 };
            var mehl = new Resource { Name = "Mehl", SellPrice = 5m, ChainOrder = 1 };
            var brot = new Resource { Name = "Brot", SellPrice = 15m, ChainOrder = 2 };

            db.Resources.AddRange(weizen, mehl, brot);
            db.SaveChanges();

            // === GEBAEUDE ===
            var feld = new Building
            {
                Name = "Feld",
                Description = "Produziert Weizen aus dem Nichts.",
                BaseCost = 0m, // Startgebaeude, kostenlos
                BaseProductionRate = 1.0, // 1 Weizen pro Sekunde
                InputResourceId = null, // Kein Input noetig
                OutputResourceId = weizen.Id,
                InputPerOutput = 0
            };

            var muehle = new Building
            {
                Name = "Muehle",
                Description = "Verarbeitet Weizen zu Mehl.",
                BaseCost = 500m,
                BaseProductionRate = 0.5, // 0.5 Mehl pro Sekunde
                InputResourceId = weizen.Id,
                OutputResourceId = mehl.Id,
                InputPerOutput = 2.0 // 2 Weizen = 1 Mehl
            };

            var baeckerei = new Building
            {
                Name = "Baeckerei",
                Description = "Verarbeitet Mehl zu Brot.",
                BaseCost = 2000m,
                BaseProductionRate = 0.25, // 0.25 Brot pro Sekunde
                InputResourceId = mehl.Id,
                OutputResourceId = brot.Id,
                InputPerOutput = 3.0 // 3 Mehl = 1 Brot
            };

            db.Buildings.AddRange(feld, muehle, baeckerei);
            db.SaveChanges();

            // === ACHIEVEMENTS ===
            db.Achievements.AddRange(
                new Achievement
                {
                    Name = "Erste Ernte",
                    Description = "Produziere zum ersten Mal Weizen.",
                    GemReward = 1
                },
                new Achievement
                {
                    Name = "Muehlenbesitzer",
                    Description = "Kaufe die Muehle.",
                    GemReward = 2
                },
                new Achievement
                {
                    Name = "Baeckermeister",
                    Description = "Kaufe die Baeckerei.",
                    GemReward = 3
                },
                new Achievement
                {
                    Name = "Ressourcen-Manager",
                    Description = "Schalte die Ressourcenverteilung frei.",
                    GemReward = 5
                },
                new Achievement
                {
                    Name = "Erste 1000 Muenzen",
                    Description = "Erreiche 1000 Muenzen.",
                    GemReward = 2
                },
                new Achievement
                {
                    Name = "Erste 10000 Muenzen",
                    Description = "Erreiche 10000 Muenzen.",
                    GemReward = 5
                }
            );
            db.SaveChanges();
        }
    }
}