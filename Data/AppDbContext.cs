using Microsoft.EntityFrameworkCore;

namespace DiscNite.Data
{
    public class AppDbContext : DbContext
    {

        public DbSet<Models.DiscordServer> DiscordServers { get; set; }
        public DbSet<Models.FortnitePlayer> FortnitePlayers { get; set; }
        public DbSet<Models.PUBGPlayer> PUBGPlayers { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Models.DiscordServer>().HasKey(x => x.IdDiscordServer);
            modelBuilder.Entity<Models.FortnitePlayer>().HasKey(x => x.IdFortnitePlayer);

            modelBuilder.Entity<Models.DiscordServer>().Property(x => x.IdDiscordServer).ValueGeneratedOnAdd();
            modelBuilder.Entity<Models.FortnitePlayer>().Property(x => x.IdFortnitePlayer).ValueGeneratedOnAdd();
            modelBuilder.Entity<Models.PUBGPlayer>().Property(x => x.IdPUBGPlayer).ValueGeneratedOnAdd();

            modelBuilder.Entity<Models.DiscordServer>().Property(x => x.Nome).IsRequired();
            modelBuilder.Entity<Models.DiscordServer>().Property(x => x.Descricao).IsRequired();
            modelBuilder.Entity<Models.DiscordServer>().Property(x => x.IdDiscord).IsRequired();
            modelBuilder.Entity<Models.DiscordServer>().Property(x => x.IdTextChannel).IsRequired();

            modelBuilder.Entity<Models.FortnitePlayer>().Property(x => x.Nome).IsRequired();
            modelBuilder.Entity<Models.FortnitePlayer>().Property(x => x.IdDiscord).IsRequired();
            modelBuilder.Entity<Models.FortnitePlayer>().Property(x => x.IdDiscordServer).IsRequired();

            modelBuilder.Entity<Models.PUBGPlayer>().Property(x => x.Nome).IsRequired();
            modelBuilder.Entity<Models.PUBGPlayer>().Property(x => x.IdDiscord).IsRequired();
            modelBuilder.Entity<Models.PUBGPlayer>().Property(x => x.IdDiscordServer).IsRequired();

            modelBuilder.Entity<Models.DiscordServer>().HasMany(x => x.FortnitePlayers).WithOne(x => x.DiscordServer).HasForeignKey(x => x.IdDiscordServer);
            modelBuilder.Entity<Models.DiscordServer>().HasMany(x => x.PUBGPlayers).WithOne(x => x.DiscordServer).HasForeignKey(x => x.IdDiscordServer);

            base.OnModelCreating(modelBuilder);
        }

    }
}
