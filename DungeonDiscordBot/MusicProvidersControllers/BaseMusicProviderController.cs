using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DungeonDiscordBot.Model;

namespace DungeonDiscordBot.MusicProvidersControllers;

public abstract class BaseMusicProviderController : IComparable<BaseMusicProviderController>, IEquatable<BaseMusicProviderController>
{
    public event Action<int>? AudiosProcessingStarted;
    public event Action<int, int>? AudiosProcessingProgressed;
    public event Action<int>? AudiosProcessed;
    
    public abstract string LinksDomainName { get; }
    
    private Type _providerType;

    protected BaseMusicProviderController()
    {
        _providerType = this.GetType();
    }

    /// <summary>
    /// Initializes the controller.
    /// </summary>
    /// <returns></returns>
    public abstract Task Init();

    /// <summary>
    /// Gets audios by a url.
    /// </summary>
    public abstract Task<IEnumerable<AudioQueueRecord>> GetAudiosFromLink(Uri link);

    public int CompareTo(BaseMusicProviderController? other)
    {
        if (other is null) {
            return 1;
        }

        return this._providerType != other._providerType ? 1 : 0;
    }

    public bool Equals(BaseMusicProviderController? other)
    {
        if (other is null) {
            return false;
        }

        return this._providerType != other._providerType;
    }

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
}