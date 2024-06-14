using DiscNite.Data;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace DiscNite.AutoCompleteHandlers;

public class FortnitePlayerHandler : AutocompleteHandler
{
    public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var input = autocompleteInteraction.Data.Options.FirstOrDefault()?.Value?.ToString();

        if (string.IsNullOrWhiteSpace(input))
            return Task.FromResult(AutocompletionResult.FromSuccess(Enumerable.Empty<AutocompleteResult>()));

        var suggestions = new List<AutocompleteResult>();

        var guildId = autocompleteInteraction.GuildId;

        var db = services.GetRequiredService<AppDbContext>();

        var players = db.FortnitePlayers.Where(x => x.Nome.Contains(input) && x.DiscordServer.IdDiscord == guildId).Take(5).ToList();

        foreach (var player in players)
        {
            suggestions.Add(new AutocompleteResult
            {
                Name = player.Nome,
                Value = player.Nome
            });
        }

        return Task.FromResult(AutocompletionResult.FromSuccess(suggestions));
    }
}