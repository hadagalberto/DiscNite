using DiscNite.Data;
using DiscNite.Services;
using DiscNite.Utils;
using Discord.Interactions;
using Discord.WebSocket;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(config =>
            {
                config.AddJsonFile("_config.json", false);
            })
            .ConfigureServices(services =>
            {
                // Use the Docker Compose SQL Server connection
                var connectionString = "Server=sql-server,1433;Database=DiscNite;User Id=sa;Password=PedesWord123;TrustServerCertificate=True;";
                services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionString));

                var discordClient = new DiscordSocketClient(new DiscordSocketConfig
                {
                    AlwaysDownloadUsers = true,
                    MessageCacheSize = 1000,
                });

                services.AddHangfire(config => {
                    config.UseSqlServerStorage(connectionString);
                });
                services.AddHangfireServer(p =>
                {
                    p.CancellationCheckInterval = TimeSpan.FromSeconds(1);
                    p.SchedulePollingInterval = TimeSpan.FromSeconds(1);
                    p.Queues = new[] { "default" };
                });
                services.AddSingleton<HangfireUpdater>();
                services.AddSingleton<DiscordSocketClient>(discordClient);
                services.AddSingleton<InteractionService>();
                services.AddHostedService<InteractionHandlingService>();
                services.AddHostedService<DiscordStartupService>();
                services.AddSingleton<FortniteApiService>();
            })
            .Build();

        // Migrate the database
        using (var scope = host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        // Run the application
        await host.RunAsync();
    }
}
