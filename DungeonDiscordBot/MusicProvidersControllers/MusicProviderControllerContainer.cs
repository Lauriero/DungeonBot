using System.Diagnostics.CodeAnalysis;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.MusicProviders;

namespace DungeonDiscordBot.MusicProvidersControllers;

public class MusicProviderControllerContainer : BaseMusicProviderController
{
    public BaseMusicProviderController? Instance { private get; set; }
    public override string LinksDomainName => Instance?.LinksDomainName ?? throw new NotImplementedException();
    public override string LogoUri => Instance?.LogoUri ?? throw new NotImplementedException();

    public MusicProviderControllerContainer(Type destinationType) : base(destinationType) { }
    
    public override Task InitializeAsync()
    {
        EnsureInstanceInitialized();
        Instance.AudiosProcessingStarted += OnAudiosProcessingStarted;
        Instance.AudiosProcessingProgressed += OnAudiosProcessingProgress;
        Instance.AudiosProcessed += OnAudiosProcessed;
        return Instance.InitializeAsync();
    }

    public override Task<MusicCollectionResponse> GetAudiosFromLinkAsync(Uri link, int count)
    {
        EnsureInstanceInitialized();
        return Instance.GetAudiosFromLinkAsync(link, count);
    }

    public override Task<MusicCollectionResponse> GetAudioFromSearchQueryAsync(string query)
    {
        EnsureInstanceInitialized();
        return Instance.GetAudioFromSearchQueryAsync(query);
    }

    public override Task<MusicSearchResult> SearchAsync(string query, MusicCollectionType targetCollectionType)
    {
        EnsureInstanceInitialized();
        return Instance.SearchAsync(query, targetCollectionType);
    }

    [MemberNotNull(nameof(Instance))]
    private void EnsureInstanceInitialized()
    {
        if (Instance is null) {
            throw new NotImplementedException();
        }
    }
}