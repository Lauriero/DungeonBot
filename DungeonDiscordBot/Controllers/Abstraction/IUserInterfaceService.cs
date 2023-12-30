﻿using System.Collections.Concurrent;

using Discord;
using Discord.WebSocket;

using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.MusicProvidersControllers;

namespace DungeonDiscordBot.Controllers;

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
    Task CreateSongsQueueMessageAsync(ulong guildId, ConcurrentQueue<AudioQueueRecord> queue,
        MusicPlayerMetadata playerMetadata, SocketTextChannel musicControlChannel, CancellationToken token = default);
 
    /// <summary>
    /// Updates the current message that is used to control music.
    /// </summary>
    Task UpdateSongsQueueMessageAsync(ulong guildId, ConcurrentQueue<AudioQueueRecord> queue, 
        MusicPlayerMetadata playerMetadata, string message = "", CancellationToken token = default);

    MessageProperties GenerateTrackHistoryMessage(ConcurrentStack<AudioQueueRecord> previousTracks);
    
    MessageProperties GenerateMissingPermissionsMessage(
        string description,
        ChannelPermission[] requiredPermissions,
        SocketGuildChannel channel);

    MessageProperties GenerateMusicServiceNotFoundMessage(IUser botUser, string userQuery);

    MessageProperties GenerateMusicServiceLinkNotSupportedMessage(BaseMusicProviderController providerControllerUsed, string userQuery);
    
    MessageProperties GenerateNewUserMessage(IUser botUser, IUser joinedUser);
    MessageProperties GenerateLeftUserMessage(IUser botUser, IUser leftUser);

}