using System.Diagnostics.CodeAnalysis;

using Discord;
using Discord.Interactions;

using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.Model.MusicProviders.Search;
using DungeonDiscordBot.MusicProvidersControllers;

namespace DungeonDiscordBot.AutocompleteHandlers;

public class SearchAutocompleteHandler : AutocompleteHandler
{
    public const string QUERY_PARAMETER_NAME = "query";
    public const string SERVICE_PARAMETER_NAME = "service";
    public const string TARGET_COLLECTION_TYPE_PARAMETER_NAME = "search-for";

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        BaseMusicProviderController providerController;
        if (!TryGetOptionByParameterName(autocompleteInteraction, SERVICE_PARAMETER_NAME,
                out AutocompleteOption? serviceOption) || serviceOption.Value is not string serviceOptionValue) {

            providerController = MusicProvider.Yandex;
        } else {
            providerController = MusicProvider.FromName(serviceOptionValue);
        }

        MusicCollectionType targetCollectionType = MusicCollectionType.Track;
        if (TryGetOptionByParameterName(autocompleteInteraction, TARGET_COLLECTION_TYPE_PARAMETER_NAME,
                out AutocompleteOption? searchForOption) && searchForOption.Value is string searchForOptionValue) {
            targetCollectionType = Enum.Parse<MusicCollectionType>(searchForOptionValue);
        }

        string query = autocompleteInteraction.Data.Current.Value.ToString()!;
        if (string.IsNullOrWhiteSpace(query)) {
            return AutocompletionResult.FromSuccess();
        }

        MusicSearchResult searchResults = await providerController.SearchAsync(query, targetCollectionType);
        List<AutocompleteResult> results = new List<AutocompleteResult>();
        foreach (SearchResultEntity entity in searchResults.Entities.Take(25)) {
            string name = entity.Name;
            if (name.Length > 100) {
                name = name.Substring(0, 100);
            }
            
            results.Add(new AutocompleteResult(name, entity.Link));
        }

        return AutocompletionResult.FromSuccess(results);
    }
    
    public static bool TryGetOptionByParameterName(IAutocompleteInteraction interaction, string parameterName, 
        [MaybeNullWhen(false)]
        out AutocompleteOption option)
    {
        foreach (AutocompleteOption dataOption in interaction.Data.Options) {
            if (dataOption.Name == parameterName) {
                option = dataOption;
                return true;
            }
        }

        option = null;
        return false;
    } 
}