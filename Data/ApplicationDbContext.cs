using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SEW04_Projekt_Bsteh.Models;

namespace SEW04_Projekt_Bsteh.Data
{
    // IdentityDbContext<ApplicationUser> statt IdentityDbContext
    // damit unser eigener User verwendet wird
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

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

            builder.Entity<Farm>()
                .HasIndex(f => f.UserId)
                .IsUnique();

            builder.Entity<UserBuilding>()
                .HasIndex(ub => new { ub.FarmId, ub.BuildingId })
                .IsUnique();

            builder.Entity<UserResource>()
                .HasIndex(ur => new { ur.FarmId, ur.ResourceId })
                .IsUnique();

            builder.Entity<ResourceAllocation>()
                .HasIndex(ra => new { ra.FarmId, ra.ResourceId })
                .IsUnique();

            builder.Entity<UserAchievement>()
                .HasIndex(ua => new { ua.FarmId, ua.AchievementId })
                .IsUnique();
        }
    }
}