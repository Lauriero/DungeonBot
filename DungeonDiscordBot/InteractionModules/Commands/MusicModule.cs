using System.Collections.Concurrent;

using Discord;
using Discord.Audio;
using Discord.Interactions;
using Discord.WebSocket;

using DungeonDiscordBot.AutocompleteHandlers;
using DungeonDiscordBot.Controllers;
using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Exceptions;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.Model.MusicProviders.Search;
using DungeonDiscordBot.MusicProvidersControllers;
using DungeonDiscordBot.Utilities;

using Microsoft.Extensions.Logging;

namespace DungeonDiscordBot.InteractionModules.Commands;

public class MusicModule : BaseInteractionModule<SocketInteractionContext>
{
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels = new();
    
    private readonly ILogger<MusicModule> _logger;
    private readonly IDiscordBotService _botService;
    private readonly IDataStorageService _dataStorageService;
    private readonly IDiscordAudioService _audioService;
    private readonly IUserInterfaceService _UIService;

    public MusicModule(ILogger<MusicModule> logger, IDiscordAudioService audioService, 
        IDiscordBotService botService, IDataStorageService dataStorageService, IUserInterfaceService uiService)
        : base(logger)
    {
        _logger = logger;
        _botService = botService;
        _audioService = audioService;
        _dataStorageService = dataStorageService;
        _UIService = uiService;
    }

    [SlashCommand(
        name:        "play",
        description: "Plays song or playlist from the link", 
        runMode:     RunMode.Async)]
    public async Task PlayAsync(
        [Summary("query", "Link to a song, playlist, video")]
        [Autocomplete(typeof(QueryAutocompleteHandler))]
        string query,

        [Summary("quantity", "Number of tracks that should be fetched")]
        int quantity = -1,
        
        [Summary("shuffle", "Flat to mix up the tracks")]
        bool shuffle = false,
        
        [Summary("now", "Flag to put the fetched songs in the head of the playlist")]
        bool now = false
    )
    {
        await MethodWrapper(async () => {
            await _botService.EnsureBotIsReady(Context.Interaction);
            await DeferAsync();
            await EnsureInMusicChannel();
            
            SocketVoiceChannel? targetChannel = Context.GetVoiceChannelWithCurrentUser();
            if (targetChannel is null) {
                await ModifyOriginalResponseAsync(m => m.Content = "User is not found in any of the voice channels");
                return;
            }

            if (!targetChannel.CheckChannelPermissions(ChannelPermissionsCatalogue.ForVoiceChannel)) {
                MessageProperties missingPermissionsMessage = _UIService.GenerateMissingPermissionsMessage(
                    $"Bot should have following permissions in the channel <#{targetChannel.Id}> in order to play music",
                    ChannelPermissionsCatalogue.ForVoiceChannel,
                    targetChannel);
                await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(missingPermissionsMessage));
                return;
            }

            if (!Uri.TryCreate(query, UriKind.Absolute, out Uri? link) 
                || link.Scheme != Uri.UriSchemeHttp && link.Scheme != Uri.UriSchemeHttps) {
                
                await ModifyOriginalResponseAsync(m => m.Content =
                    $"***{query} is not an url***");
                return;
            }

            await PlayByUrlAsync(link, targetChannel, quantity, shuffle, now);
        });
    }
    

    [SlashCommand(name: "search",
        description: "Searches music in the specified music service",
        runMode: RunMode.Async)]
    public async Task SearchAsync(
        [Summary(SearchAutocompleteHandler.SERVICE_PARAMETER_NAME, "Music service to search in")]
        MusicProvider provider,
        
        [Summary(SearchAutocompleteHandler.QUERY_PARAMETER_NAME, "Search query")]
        [Autocomplete(typeof(SearchAutocompleteHandler))]
        string query,
        
        [Summary(SearchAutocompleteHandler.TARGET_COLLECTION_TYPE_PARAMETER_NAME, "Type of the entity to search for")]
        MusicCollectionType searchFor = MusicCollectionType.Track,
        
        [Summary("shuffle", "Flag to mix up the tracks")]
        bool shuffle = false,
        
        [Summary("now", "Flag to put the fetched songs in the head of the playlist")]
        bool now = false)
    {
        await MethodWrapper(async () => {
            await _botService.EnsureBotIsReady(Context.Interaction);
            await DeferAsync();
            await EnsureInMusicChannel();
            
            SocketVoiceChannel? targetChannel = Context.GetVoiceChannelWithCurrentUser();
            if (targetChannel is null) {
                await ModifyOriginalResponseAsync(m => m.Content = "User is not found in any of the voice channels");
                return;
            }

            if (!targetChannel.CheckChannelPermissions(ChannelPermissionsCatalogue.ForVoiceChannel)) {
                MessageProperties missingPermissionsMessage = _UIService.GenerateMissingPermissionsMessage(
                    $"Bot should have following permissions in the channel <#{targetChannel.Id}> in order to play music",
                    ChannelPermissionsCatalogue.ForVoiceChannel,
                    targetChannel);
                await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(missingPermissionsMessage));
                return;
            }

            if (!Uri.TryCreate(query, UriKind.Absolute, out Uri? link) 
                || link.Scheme != Uri.UriSchemeHttp && link.Scheme != Uri.UriSchemeHttps) {

                await ModifyOriginalResponseAsync(m => m.Content =
                    $"***{query} is not an url***");

                return;
            }

            await PlayByUrlAsync(link, targetChannel, -1, shuffle, now);
        });
    }

    [SlashCommand("gachify", "Does some gachi magic", runMode: RunMode.Async)]
    public async Task GachifyAsync()
    {
        await MethodWrapper(async () => {
            await _botService.EnsureBotIsReady(Context.Interaction);
            await DeferAsync();
            await EnsureInMusicChannel();

            List<string> trackNames = new List<string>();
            MusicPlayerMetadata metadata = _audioService.GetMusicPlayerMetadata(Context.Guild.Id);
            foreach (AudioQueueRecord track in metadata.Queue) {
                trackNames.Add(new string(track.Title));
            }
            
            await _audioService.ClearQueue(Context.Guild.Id);
            await ModifyOriginalResponseAsync(m =>
                m.Content = $"**Unstoppable** ***gachification*** **process has been started**\n" +
                            $"**To gachify**: ***{trackNames.Count}*** **tracks**");

            List<AudioQueueRecord> resultTracks = new List<AudioQueueRecord>();
            BaseMusicProviderController vkProvider = MusicProvider.VK.Value;
            foreach (string trackName in trackNames) {
                MusicSearchResult searchResult = await vkProvider.SearchAsync($"{trackName} right version", MusicCollectionType.Track, 1);
                if (!searchResult.Entities.Any()) {
                    continue;
                }

                SearchResultEntity resultEntity = searchResult.Entities.First();
                if (!resultEntity.Name.Contains("♂") && 
                    !resultEntity.Name.Contains("right version", StringComparison.OrdinalIgnoreCase) ||
                    !resultEntity.Name.Contains(trackName, StringComparison.OrdinalIgnoreCase)) {
                    
                    continue;
                }

                MusicCollectionResponse response = await vkProvider.GetAudiosFromLinkAsync(new Uri(resultEntity.Link), 1);
                if (response.IsError || !response.Audios.Any()) {
                    continue;
                }

                resultTracks.Add(response.Audios.First());
            }
            
            await ModifyOriginalResponseAsync(m =>
                m.Content = $"**Your queue has been gachified." +
                            $"Transformed {resultTracks.Count} tracks out of {trackNames.Count} tracks**");
            
            await _audioService.AddAudios(Context.Guild.Id, resultTracks, false);
            await _audioService.PlayQueueAsync(Context.Guild.Id, 
                $"**{resultTracks.Count}** gachi tracks were added to the queue");
        });
    }

    [SlashCommand("history", 
        "Shows the music playback history, allowing to queue one of the previous tracks",
        runMode: RunMode.Async)]
    public async Task HistoryAsync()
    {
        await MethodWrapper(async () => {
            await DeferAsync(true);
            await EnsureInMusicChannel();

            MusicPlayerMetadata playerMetadata = _audioService.GetMusicPlayerMetadata(Context.Guild.Id);
            await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(
                _UIService.GenerateTrackHistoryMessage(playerMetadata.PreviousTracks)));
        }, false);
    }

    [SlashCommand("remove",
        "Removes the track from the queue",
        runMode: RunMode.Async)]
    public async Task RemoveAsync(
        [Summary("number", "Number of the track in the queue that needs to be removed")]
        int position)
    {
        await MethodWrapper(async () => {
            await DeferAsync();
            await EnsureInMusicChannel();
            
            
        });
    }

    [SlashCommand("clean",
        "Cleans up the music control channel, leaving only the music control message",
        runMode: RunMode.Async)]
    public async Task CleanAsync(
        [Summary("limit", "The maximum number of messages to be removed")]
        int limit = 100)
    {
        await MethodWrapper(async () => {
            await DeferAsync(true);
            await EnsureInMusicChannel();

            Guild guild = await _dataStorageService.GetGuildDataAsync(Context.Guild.Id);
            IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(limit)
                .FlattenAsync();

            DateTimeOffset minimum = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14));
            IEnumerable<IMessage> messagesToDelete = messages.Where(m => m.Id != guild.MusicMessageId && 
                                                                         m.CreatedAt > minimum);
            
            await ((ITextChannel) Context.Channel).DeleteMessagesAsync(messagesToDelete);
            await ModifyOriginalResponseAsync(m =>
                m.Content = $"**{messagesToDelete.Count()} messages have been removed**");
        }, false);
    }

    private async Task PlayByUrlAsync(Uri link, SocketVoiceChannel targetChannel, int quantity = -1,
        bool shuffle = false, bool now = false)
    {
        BaseMusicProviderController? controller = link.FindMusicProviderController();
        if (controller is null) {
            await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(
                _UIService.GenerateMusicServiceNotFoundMessage(Context.Guild.CurrentUser, link.AbsoluteUri)));
            return;
        }
        
        MusicCollectionResponse collection = await controller.GetAudiosFromLinkAsync(link, quantity);
        if (collection.IsError) {
            _logger.LogInformation($"Error while getting music from {collection.Provider.Name} music provider " +
                                   $"[guildId: {Context.Guild.Id}; query: {link.AbsoluteUri}]: " +
                                   $"{collection.ErrorType} - {collection.ErrorMessage}");
            switch (collection.ErrorType) {
                case MusicResponseErrorType.PermissionDenied:
                    await ModifyOriginalResponseAsync((m) 
                        => m.Content = $"Permission to audio was denied");
                    return;
                
                case MusicResponseErrorType.NoAudioFound:
                    await ModifyOriginalResponseAsync((m) 
                        => m.Content = $"No audio was found by the requested url");
                    return;
                
                case MusicResponseErrorType.LinkNotSupported:
                    await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(
                        _UIService.GenerateMusicServiceLinkNotSupportedMessage(controller, link.AbsoluteUri)));
                    return;
                default:
                    return;
            }
        }

        if (shuffle) {
            collection.Audios.Shuffle();
        }
        
        await ModifyOriginalResponseAsync(m => m.Content = 
            $"Found {collection.Audios.Count} audios");

        _audioService.GetMusicPlayerMetadata(Context.Guild.Id).VoiceChannel = targetChannel;
        await _dataStorageService.RegisterMusicQueryAsync(Context.Guild.Id, collection.Name, link.AbsoluteUri);
        await _audioService.AddAudios(Context.Guild.Id, collection.Audios, now);
        await _audioService.PlayQueueAsync(Context.Guild.Id, $"**{collection.Audios.Count()}** tracks from {collection.Name} were added to the queue");
    }

    private async Task EnsureInMusicChannel()
    {
        Guild guild = await _dataStorageService.GetGuildDataAsync(Context.Guild.Id);
        if (guild.MusicChannelId is null || guild.MusicMessageId is null) {
            await ModifyOriginalResponseAsync(m =>
                m.Content = "Music channel is not registered, register it with /register-music-channel");
            throw new MusicChannelNotRegisteredException();
        }

        if (Context.Channel.Id != guild.MusicChannelId.Value) {
            await ModifyOriginalResponseAsync(m =>
                m.Content = "Music player commands can only be executed in the preregistered music channel");
            throw new InteractionCommandException(Context.Interaction, InteractionCommandError.Exception,
                "Attempt to execute command of a music module in the channel, " +
                "that is not registered as a music channel");
        }
    }
}