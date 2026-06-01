using GameServer.Models;
using Microsoft.EntityFrameworkCore;

namespace GameServer.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Country> Countries { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Move> Moves { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.IdentityNumber)
                .IsUnique();

            modelBuilder.Entity<Player>()
                .ToTable(t => t.HasCheckConstraint("CK_Player_IdentityNumber",
                    "IdentityNumber BETWEEN 1 AND 1000"));

            modelBuilder.Entity<Player>()
                .ToTable(t => t.HasCheckConstraint("CK_Player_Phone",
                    "LEN(Phone) = 10"));

            modelBuilder.Entity<Game>()
                .HasOne(g => g.Player)
                .WithMany(p => p.Games)
                .HasForeignKey(g => g.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Move>()
                .HasOne(m => m.Game)
                .WithMany(g => g.Moves)
                .HasForeignKey(m => m.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}