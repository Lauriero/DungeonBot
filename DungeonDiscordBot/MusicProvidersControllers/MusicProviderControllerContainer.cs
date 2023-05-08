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
        return Instance.InitializeAsync();
    }

    public override Task<IEnumerable<AudioQueueRecord>> GetAudiosFromLink(Uri link)
    {
        EnsureInstanceInitialized();
        return Instance.GetAudiosFromLink(link);
    }

    [MemberNotNull(nameof(Instance))]
    private void EnsureInstanceInitialized()
    {
        if (Instance is null) {
            throw new NotImplementedException();
        }
    }
}