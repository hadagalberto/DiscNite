using Discord;
using Discord.Interactions;

namespace DiscNite.Commands
{
    public class UtilsModule : InteractionModuleBase<SocketInteractionContext>
    {

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
                .WithDescription("DiscNite é um bot para o Discord que fornece informações sobre o Fortnite")
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

    }
}
