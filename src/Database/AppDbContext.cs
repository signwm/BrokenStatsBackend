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
        public DbSet<InstanceEntity> Instances => Set<InstanceEntity>();
        public DbSet<BreakEntity> Breaks => Set<BreakEntity>();


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
            modelBuilder.Entity<FightEntity>()
                .HasOne(f => f.Instance)
                .WithMany()
                .HasForeignKey(f => f.InstanceId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<InstanceEntity>().HasIndex(i => i.InstanceId).IsUnique();
            modelBuilder.Entity<InstanceEntity>().Property(i => i.StartTime).HasColumnType("DATETIME");
            modelBuilder.Entity<InstanceEntity>().Property(i => i.EndTime).HasColumnType("DATETIME");
            modelBuilder.Entity<BreakEntity>().Property(b => b.StartTime).HasColumnType("DATETIME");
            modelBuilder.Entity<BreakEntity>().Property(b => b.EndTime).HasColumnType("DATETIME");

        }
    }
}