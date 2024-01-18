using System.Collections.Concurrent;

using DungeonDiscordBot.Storage.Abstraction;

namespace DungeonDiscordBot.Storage;

/// <inheritdoc />
public class SelectedOptionsStorage : ISelectedOptionsStorage
{
    /// <inheritdoc />
    public ConcurrentDictionary<ulong, string> HistoryMessage { get; }
    
    /// <inheritdoc />
    public ConcurrentDictionary<ulong, string> FavoritesMessage { get; }

    public SelectedOptionsStorage()
    {
        HistoryMessage = new ConcurrentDictionary<ulong, string>();
        FavoritesMessage = new ConcurrentDictionary<ulong, string>();
    }
}