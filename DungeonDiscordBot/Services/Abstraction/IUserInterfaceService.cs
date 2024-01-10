using System.Collections.Concurrent;

using Discord;
using Discord.WebSocket;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.MusicProvidersControllers;

namespace DungeonDiscordBot.Services.Abstraction;

public interface IUserInterfaceService
{
    /// <summary>
    /// Returns the number of bars used to display player progress bar.
    /// </summary>
    public int ProgressBarsCount { get; }
    
    /// <summary>
    /// Creates a new message that is used to control music
    /// and sends it to the music control channel,
    /// storing channel and message ids.
    /// </summary>
    /// <returns>ID of the created message with a queue.</returns>
    Task<ulong> CreateSongsQueueMessageAsync(ulong guildId, MusicPlayerMetadata playerMetadata, 
        SocketTextChannel musicControlChannel, CancellationToken token = default);
 
    /// <summary>
    /// Updates the current message that is used to control music.
    /// </summary>
    Task UpdateSongsQueueMessageAsync(ulong guildId, MusicPlayerMetadata playerMetadata, 
        string message = "", CancellationToken token = default);

    MessageProperties GenerateTrackHistoryMessage(ConcurrentStack<AudioQueueRecord> previousTracks, 
        string? selectedTrackUri = null);

    public MessageProperties GenerateUserFavoritesMessage(List<FavoriteMusicCollection> favorites,
        string? selectedCollectionQuery = null);
    
    MessageProperties GenerateMissingPermissionsMessage(
        string description,
        ChannelPermission[] requiredPermissions,
        SocketGuildChannel channel);

    MessageProperties GenerateMusicServiceNotFoundMessage(IUser botUser, string userQuery);

    MessageProperties GenerateMusicServiceLinkNotSupportedMessage(BaseMusicProviderController providerControllerUsed, string userQuery);
    
    MessageProperties GenerateNewUserMessage(IUser botUser, IUser joinedUser);
    MessageProperties GenerateLeftUserMessage(IUser botUser, IUser leftUser);

}