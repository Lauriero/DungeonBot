using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.MusicProviders;

namespace DungeonDiscordBot.MusicProvidersControllers;

public abstract class BaseMusicProviderController : 
    IComparable<BaseMusicProviderController>, 
    IEquatable<BaseMusicProviderController>,
    IRequireInitiationService
{
    public event Action<int>? AudiosProcessingStarted;
    public event Action<int, int>? AudiosProcessingProgressed;
    public event Action<int>? AudiosProcessed;
    
    public abstract string LinksDomainName { get; }
    
    public abstract string LogoUri { get; }

    public int InitializationPriority => 0;
    
    public Type ProviderType { get; protected set; }
    
    protected BaseMusicProviderController(Type? destinationType = null)
    {
        ProviderType = destinationType ?? this.GetType();
    }

    public abstract Task InitializeAsync();

    /// <summary>
    /// Gets audios by a url.
    /// </summary>
    public abstract Task<MusicCollectionResponse> GetAudiosFromLinkAsync(Uri link, int count);

    /// <summary>
    /// Gets a single audio from a search query.
    /// </summary>
    public abstract Task<MusicCollectionResponse> GetAudioFromSearchQueryAsync(string query);

    protected void OnAudiosProcessingStarted(int audiosToProcess)
    {
        AudiosProcessingStarted?.Invoke(audiosToProcess);
    }

    protected void OnAudiosProcessingProgress(int audiosProcessed, int audiosToProcess)
    {
        AudiosProcessingProgressed?.Invoke(audiosProcessed, audiosToProcess);
    }

    protected void OnAudiosProcessed(int audiosAdded)
    {
        AudiosProcessed?.Invoke(audiosAdded);
    }
    
    #region Implementations

    public int CompareTo(BaseMusicProviderController? other)
    {
        if (other is null) {
            return 1;
        }

        return this.ProviderType != other.ProviderType ? 1 : 0;
    }

    public bool Equals(BaseMusicProviderController? other)
    {
        if (other is null) {
            return false;
        }

        return this.ProviderType != other.ProviderType;
    }
    
    #endregion
    
}