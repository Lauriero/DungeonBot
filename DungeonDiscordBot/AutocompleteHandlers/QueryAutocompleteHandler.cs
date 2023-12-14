using Discord;
using Discord.Interactions;

using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Model.Database;

namespace DungeonDiscordBot.AutocompleteHandlers;

public class QueryAutocompleteHandler : AutocompleteHandler
{
    private readonly IDataStorageService _dataStorage;
    
    public QueryAutocompleteHandler(IDataStorageService dataStorage)
    {
        _dataStorage = dataStorage;
    }
    
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        IEnumerable<MusicQueryHistoryEntity> results = await _dataStorage.GetLastMusicQueries(context.Guild.Id);
        return AutocompletionResult.FromSuccess(results
            .Select(q => new AutocompleteResult(q.QueryName, q.QueryValue))
            .Take(25));
    }
}