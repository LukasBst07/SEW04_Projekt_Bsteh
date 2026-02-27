using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using SEW04_Projekt_Bsteh.Models;

namespace SEW04_Projekt_Bsteh.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Spiel-Tabellen
        public DbSet<Farm> Farms => Set<Farm>();
        public DbSet<Building> Buildings => Set<Building>();
        public DbSet<UserBuilding> UserBuildings => Set<UserBuilding>();
        public DbSet<Resource> Resources => Set<Resource>();
        public DbSet<UserResource> UserResources => Set<UserResource>();
        public DbSet<ResourceAllocation> ResourceAllocations => Set<ResourceAllocation>();
        public DbSet<Achievement> Achievements => Set<Achievement>();
        public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Farm: Ein User hat genau eine Farm
            builder.Entity<Farm>()
                .HasIndex(f => f.UserId)
                .IsUnique();

            // UserBuilding: Ein Gebaeude pro Farm nur einmal
            builder.Entity<UserBuilding>()
                .HasIndex(ub => new { ub.FarmId, ub.BuildingId })
                .IsUnique();

            // UserResource: Eine Ressource pro Farm nur einmal
            builder.Entity<UserResource>()
                .HasIndex(ur => new { ur.FarmId, ur.ResourceId })
                .IsUnique();

            // ResourceAllocation: Eine Verteilung pro Farm und Ressource
            builder.Entity<ResourceAllocation>()
                .HasIndex(ra => new { ra.FarmId, ra.ResourceId })
                .IsUnique();

            // UserAchievement: Ein Achievement pro Farm nur einmal
            builder.Entity<UserAchievement>()
                .HasIndex(ua => new { ua.FarmId, ua.AchievementId })
                .IsUnique();
        }
    }
}
