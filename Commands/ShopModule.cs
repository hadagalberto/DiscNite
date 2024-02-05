using DiscNite.Services;
using Discord;
using Discord.Interactions;

namespace DiscNite.Commands
{
    public class ShopModule : InteractionModuleBase<SocketInteractionContext>
    {

        private readonly FortniteApiService _fortniteApiService;

        public ShopModule(FortniteApiService fortniteApiService)
        {
            _fortniteApiService = fortniteApiService;
        }

        [SlashCommand("shop", "Mostra a loja atual do Fortnite")]
        public async Task ShopAsync()
        {
            var shop = await _fortniteApiService.GetShopAsync();

            var itensToShow = shop.Daily.Entries.Take(5);

            var builder = new ComponentBuilder()
                .WithButton("Próxima página", "next", ButtonStyle.Primary)
                .WithButton("Página anterior", "previous", ButtonStyle.Primary);

            var embed = new EmbedBuilder()
                .WithTitle("Loja do Fortnite")
                .WithDescription("Aqui estão os itens da loja atual do Fortnite")
                .WithColor(Color.Blue)
                .WithImageUrl(itensToShow.FirstOrDefault().Items.FirstOrDefault().Images.Featured.ToString())
                .WithFooter("Loja atual do Fortnite")
                .Build();

            await RespondAsync(embeds: new[] { embed }, components: builder.Build());
        }

    }
}
