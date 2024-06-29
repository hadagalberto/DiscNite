using System.ComponentModel;
using DiscNite.Data;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace DiscNite.Commands
{

    [Description("Comandos de administração")]
    public class AdministrationModule : InteractionModuleBase<SocketInteractionContext>
    {

        private readonly AppDbContext _dbContext;

        public AdministrationModule(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [SlashCommand("ping", "Pong!")]
        public async Task PingAsync()
        {
            await RespondAsync("Pong!");
        }

        [SlashCommand("info", "Mostra informações sobre o bot")]
        public async Task InfoAsync()
        {
            var description = "DiscNite é um bot para o Discord que fornece informações sobre o Fortnite";

            description += "\n\n";

            description += "Ele pode mostrar a loja atual do Fortnite, estatísticas de jogadores e muito mais!";

            description += "\n\n";

            description += "Para ver todos os comandos disponíveis, digite /help";

            var embed = new EmbedBuilder()
                .WithTitle("DiscNite")
                .WithDescription(description)
                .WithColor(Color.Blue)
                .WithFooter("DiscNite")
                .Build();

            await RespondAsync(embeds: new[] { embed });
        }

        [SlashCommand("help", "Mostra todos os comandos disponíveis")]
        public async Task HelpAsync()
        {
            var builder = new ComponentBuilder()
                .WithButton("Loja", "shop", ButtonStyle.Primary)
                .WithButton("Estatísticas", "stats", ButtonStyle.Primary);

            var embed = new EmbedBuilder()
                .WithTitle("Comandos disponíveis")
                .WithDescription("Aqui estão todos os comandos disponíveis")
                .WithColor(Color.Blue)
                .WithFooter("Comandos disponíveis")
                .Build();

            await RespondAsync(embeds: new[] { embed }, components: builder.Build());
        }

        [SlashCommand("updatechannel", "Atualiza o canal de texto onde as mensagens serão enviadas")]
        public async Task UpdateChannel()
        {
            var guidId = this.Context.Guild.Id;

            var serverInDb = await _dbContext.DiscordServers.FirstOrDefaultAsync(x => x.IdDiscord == guidId);

            if (serverInDb == null)
            {
                await RespondAsync("Não estamos acompanhando nenhum player ❌");
                return;
            }

            serverInDb.IdTextChannel = this.Context.Channel.Id;

            _dbContext.DiscordServers.Update(serverInDb);
            await _dbContext.SaveChangesAsync();

            await RespondAsync($"Canal atualizado para {this.Context.Channel.Name}");
        }
    }
}