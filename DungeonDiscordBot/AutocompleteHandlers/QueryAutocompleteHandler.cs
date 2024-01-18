using Discord;
using Discord.Interactions;

using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.Services.Abstraction;

namespace DungeonDiscordBot.AutocompleteHandlers;

public class QueryAutocompleteHandler : AutocompleteHandler
{
    private readonly IDataStorageService _dataStorage;
    
    public QueryAutocompleteHandler(IDataStorageService dataStorage)
    {
        _dataStorage = dataStorage;
    }
    
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, 
        IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        string? query = autocompleteInteraction.Data.Current.Value.ToString();
        
        List<FavoriteMusicCollection> favorites =
            await _dataStorage.FavoriteCollections.GetAsync(context.User.Id);

        IEnumerable<MusicQueryHistoryEntity> queries = await _dataStorage.MusicHistory.GetLastMusicQueries(context.Guild.Id,
            25 - favorites.Count);
        
        List<AutocompleteResult> autocompleteResults = favorites
            .Select(c => new AutocompleteResult($"★ {c.CollectionName}", c.Query))
            .ToList();

        autocompleteResults.AddRange(queries
            .Select(q => new AutocompleteResult(q.QueryName, q.QueryValue)));
        
        if (!string.IsNullOrEmpty(query)) {
            autocompleteResults.RemoveAll(r =>
                !r.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        } 

        return AutocompletionResult.FromSuccess(autocompleteResults
            .DistinctBy(r => r.Value)
            .Take(25));
    }
}