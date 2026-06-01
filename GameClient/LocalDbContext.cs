using GameClient.Models;
using System.Data.Entity;

namespace GameClient
{
    public class LocalDbContext : DbContext
    {
        public LocalDbContext(string playerName)
: base($"Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=LocalGameDb_{playerName};Integrated Security=True;")
        {
            Database.SetInitializer(
                new CreateDatabaseIfNotExists<LocalDbContext>());
        }

        public DbSet<LocalGame> Games { get; set; }
        public DbSet<LocalMove> Moves { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LocalGame>()
                .HasMany(g => g.Moves)
                .WithRequired(m => m.Game)
                .HasForeignKey(m => m.GameId)
                .WillCascadeOnDelete(true);
        }
    }
}