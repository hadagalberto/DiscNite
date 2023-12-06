using DiscNite.Data;
using DiscNite.Util;
using DiscNite.Utils;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DiscNite.Services
{
    public class InteractionHandlingService : IHostedService
    {
        private readonly DiscordSocketClient _discord;
        private readonly InteractionService _interactions;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _config;
        private readonly ILogger<InteractionService> _logger;
        private readonly AppDbContext _db;
        private readonly FortniteApiService _fortniteApiService;

        public InteractionHandlingService(
            DiscordSocketClient discord,
            InteractionService interactions,
            IServiceProvider services,
            IConfiguration config,
            ILogger<InteractionService> logger,
            AppDbContext db,
            FortniteApiService fortniteApiService)
        {
            _discord = discord;
            _interactions = interactions;
            _services = services;
            _config = config;
            _logger = logger;
            _db = db;
            _fortniteApiService = fortniteApiService;

            _interactions.Log += msg => LogHelper.OnLogAsync(_logger, msg);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _discord.Ready += () => _interactions.RegisterCommandsGloballyAsync(true);
            _discord.InteractionCreated += OnInteractionAsync;

            await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            var updater = new HangfireUpdater(_db, _fortniteApiService, _discord);

            RecurringJob.AddOrUpdate("PlayerUpdater", () => updater.UpdatePlayerStats(), Cron.Minutely);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _interactions.Dispose();
            return Task.CompletedTask;
        }

        private async Task OnInteractionAsync(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(_discord, interaction);
                var result = await _interactions.ExecuteCommandAsync(context, _services);

                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
            catch
            {
                if (interaction.Type == InteractionType.ApplicationCommand)
                {
                    await interaction.GetOriginalResponseAsync()
                        .ContinueWith(msg => msg.Result.DeleteAsync());
                }
            }
        }
    }
}
