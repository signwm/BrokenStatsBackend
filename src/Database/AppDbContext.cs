using Microsoft.EntityFrameworkCore;
using BrokenStatsBackend.src.Models;

namespace BrokenStatsBackend.src.Database
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<FightEntity> Fights => Set<FightEntity>();
        public DbSet<OpponentEntity> Opponents => Set<OpponentEntity>();
        public DbSet<OpponentTypeEntity> OpponentTypes => Set<OpponentTypeEntity>();
        public DbSet<DropEntity> Drops => Set<DropEntity>();
        public DbSet<PriceEntity> Prices => Set<PriceEntity>();
        public DbSet<DropItemEntity> DropItems => Set<DropItemEntity>();
        public DbSet<DropTypeEntity> DropTypes => Set<DropTypeEntity>();


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=data/data.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OpponentTypeEntity>().HasIndex(x => new { x.Name, x.Level }).IsUnique();
            modelBuilder.Entity<DropTypeEntity>().HasIndex(x => x.Type).IsUnique();
            modelBuilder.Entity<DropItemEntity>().HasIndex(x => new { x.Name, x.Quality }).IsUnique();
            modelBuilder.Entity<FightEntity>().HasIndex(f => f.PublicId).IsUnique();
            modelBuilder.Entity<FightEntity>().Property(f => f.Time).HasColumnType("DATETIME");

        }
    }
}