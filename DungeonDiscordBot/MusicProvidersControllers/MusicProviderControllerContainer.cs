using System.Diagnostics.CodeAnalysis;

using DungeonDiscordBot.Model;

namespace DungeonDiscordBot.MusicProvidersControllers;

public class MusicProviderControllerContainer : BaseMusicProviderController
{
    public BaseMusicProviderController? Instance { private get; set; }
    public override string LinksDomainName => Instance?.LinksDomainName ?? throw new NotImplementedException();

    
    public MusicProviderControllerContainer(Type destinationType) : base(destinationType) { }
    
    public override Task InitializeAsync()
    {
        EnsureInstanceInitialized();
        Instance.AudiosProcessingStarted += OnAudiosProcessingStarted;
        Instance.AudiosProcessingProgressed += OnAudiosProcessingProgress;
        Instance.AudiosProcessed += OnAudiosProcessed;
        return Instance.InitializeAsync();
    }

    public override Task<IEnumerable<AudioQueueRecord>> GetAudiosFromLinkAsync(Uri link)
    {
        EnsureInstanceInitialized();
        return Instance.GetAudiosFromLinkAsync(link);
    }

    public override Task<AudioQueueRecord?> GetAudioFromSearchQueryAsync(string query)
    {
        EnsureInstanceInitialized();
        return Instance.GetAudioFromSearchQueryAsync(query);
    }

    [MemberNotNull(nameof(Instance))]
    private void EnsureInstanceInitialized()
    {
        if (Instance is null) {
            throw new NotImplementedException();
        }
    }
}