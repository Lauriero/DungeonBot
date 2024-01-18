using System.Collections.Concurrent;

namespace DungeonDiscordBot.Storage.Abstraction;

/// <summary>
/// Storage to save selected options for control messages.
/// </summary>
public interface ISelectedOptionsStorage
{
    /// <summary>
    /// Stores the selected option as a value
    /// of the specific /history message with ID as a key. 
    /// </summary>
    ConcurrentDictionary<ulong, string> HistoryMessage { get; }
    
    /// <summary>
    /// Stores the selected option as a value
    /// of the specific /favorite list message with ID as a key. 
    /// </summary>
    ConcurrentDictionary<ulong, string> FavoritesMessage { get; }
}