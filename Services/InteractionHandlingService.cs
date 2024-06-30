using DiscNite.Commands;
using DiscNite.Utils;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Hangfire;
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
        private readonly ILogger<InteractionService> _logger;

        public InteractionHandlingService(
            DiscordSocketClient discord,
            InteractionService interactions,
            IServiceProvider services,
            ILogger<InteractionService> logger)
        {
            _discord = discord;
            _interactions = interactions;
            _services = services;
            _logger = logger;

            _interactions.Log += msg => LogHelper.OnLogAsync(_logger, msg);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _discord.Ready += () => _interactions.RegisterCommandsGloballyAsync();

            await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _discord.InteractionCreated += OnInteractionAsync;

            var updater = _services.GetService<HangfireUpdater>();

            BackgroundJob.Enqueue(() => updater.UpdateFortnitePlayerStats());
            RecurringJob.AddOrUpdate("PlayerUpdater", () => updater.UpdateFortnitePlayerStats(), "*/30 * * * *");
            RecurringJob.AddOrUpdate("PlayerTopFiveFortnite", () => updater.ProcessFortniteTopFiveDaily(), Cron.Daily(23));
            RecurringJob.AddOrUpdate("PlayerTopFivePUBG", () => updater.ProcessPUBGTopFiveDaily(), Cron.Daily(23));
            RecurringJob.AddOrUpdate("Atividade", () => updater.AtualizarAtividadeDiscord(), Cron.Hourly);
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
