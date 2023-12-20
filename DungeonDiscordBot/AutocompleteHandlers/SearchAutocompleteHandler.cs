using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

using Discord;
using Discord.Interactions;

using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.MusicProvidersControllers;

namespace DungeonDiscordBot.AutocompleteHandlers;

public class SearchAutocompleteHandler<TProvider> : AutocompleteHandler
    where TProvider : BaseMusicProviderController
{
    public const string QUERY_PARAMETER_NAME = "query";
    public const string SERVICE_PARAMETER_NAME = "service";
    public const string TARGET_COLLECTION_TYPE_PARAMETER_NAME = "search-for";
    
    private readonly BaseMusicProviderController _providerController;

    public SearchAutocompleteHandler()
    {
        _providerController = MusicProvider.FromProviderType(typeof(TProvider));
    }
    
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
        
        MusicSearchResult searchResults = 
            await providerController
                .SearchAsync(autocompleteInteraction.Data.Current.Value.ToString()!, targetCollectionType);
        
        return AutocompletionResult.FromSuccess(searchResults.Entities
            .Take(25)
            .Select(r => new AutocompleteResult(r.Name, r.Link)));
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