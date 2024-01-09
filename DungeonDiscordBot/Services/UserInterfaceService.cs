using System.Collections.Concurrent;
using System.Text;

using CaseConverter;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

using DungeonDiscordBot.ButtonHandlers;
using DungeonDiscordBot.InteractionModules.Components;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.MusicProvidersControllers;
using DungeonDiscordBot.Services.Abstraction;
using DungeonDiscordBot.Utilities;

namespace DungeonDiscordBot.Services;

public class UserInterfaceService : IUserInterfaceService
{
    public int ProgressBarsCount => 15;

    private readonly IDataStorageService _dataStorageService;

    public UserInterfaceService(IDataStorageService dataStorageService)
    {
        _dataStorageService = dataStorageService;
    }
    
    public async Task<ulong> CreateSongsQueueMessageAsync(ulong guildId, MusicPlayerMetadata playerMetadata,
        SocketTextChannel musicControlChannel, CancellationToken token = default)
    {
        Guild guild = await _dataStorageService.GetGuildDataAsync(guildId, token);
        
        MessageProperties musicMessage = await GenerateMusicMessageAsync(guild.Name, playerMetadata);
        RestUserMessage message = await musicControlChannel.SendMessageAsync("", 
            embed: musicMessage.Embed.Value, 
            components: musicMessage.Components.Value,
            options: new RequestOptions {
                CancelToken = token,
                RetryMode = RetryMode.Retry502 | RetryMode.RetryTimeouts
            });

        return message.Id;
    }
    
    public async Task UpdateSongsQueueMessageAsync(ulong guildId, MusicPlayerMetadata playerMetadata, 
        string message = "", CancellationToken token = default)
    {
        Guild guild = await _dataStorageService.GetGuildDataAsync(guildId, token);
        SocketTextChannel musicControlChannel = _dataStorageService.GetMusicControlChannel(guildId);
        MessageProperties musicMessage = await GenerateMusicMessageAsync(musicControlChannel.Guild.Name, playerMetadata);
        try {
            await musicControlChannel.ModifyMessageAsync(guild.MusicMessageId!.Value, m => {
                m.Content = string.IsNullOrEmpty(message) ? new Optional<string>() : message;
                m.Embed = musicMessage.Embed;
                m.Components = musicMessage.Components;
            }, new RequestOptions {
                CancelToken = token,
                RetryMode = RetryMode.Retry502 | RetryMode.RetryTimeouts
            }); 
        } catch (OperationCanceledException) {
        } catch (TimeoutException) { }
    }
    
    public MessageProperties GenerateTrackHistoryMessage(ConcurrentStack<AudioQueueRecord> previousTracks, 
        string? selectedTrackUri = null)
    {
        StringBuilder descriptionBuilder = new StringBuilder();
        IEnumerable<AudioQueueRecord> lastTracks = previousTracks.DistinctBy(t => t.PublicUrl).Take(10);
        for (int i = 0; i < 10 && i < lastTracks.Count(); i++) {
            AudioQueueRecord record = lastTracks.ElementAt(i);
            
            string title;
            if (record.PublicUrl is not null) {
                title = $"[{record.Author} - {record.Title}]({record.PublicUrl})";
            } else {
                title = $"{record.Author} - {record.Title}";
            }

            descriptionBuilder.AppendLine($"`[{i + 1}]`‎ ‎‏‏‎‎ {title}");
        }
        
        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithColor(EmbedColors.Info)
            .WithCurrentTimestamp()
            .WithThumbnailUrl("http://larc.tech/content/dungeon-bot/history-book-c1-64.png")
            .WithTitle("Recent tracks played: ")
            .WithDescription(descriptionBuilder.ToString());

        string? selectedTrackName = null;
        List<ActionRowBuilder> rows = new List<ActionRowBuilder>();
        if (!previousTracks.IsEmpty) {
            rows.Add(new ActionRowBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithPlaceholder("Select a track operate with")
                    .WithCustomId(PlaybackHistoryModule.TRACK_SELECT_ID)
                    .WithOptions(lastTracks.Select(t => {
                            bool selected = false;
                            if (selectedTrackUri == t.PublicUrl) {
                                selectedTrackName = $"{t.Author} - {t.Title}";
                                selected = true;
                            }
                            
                            return new SelectMenuOptionBuilder()
                                .WithLabel($"{t.Author} - {t.Title}")
                                .WithValue(t.PublicUrl)
                                .WithDefault(selected);
                        })
                        .ToList())));
        }
        
        rows.Add(new ActionRowBuilder()
            .WithButton(" ", emote: Emote.Parse(Emojis.REFRESH_ICON), customId: PlaybackHistoryModule.REFRESH_HISTORY_ID, style: ButtonStyle.Secondary)
            .WithButton("Play", customId: PlaybackHistoryModule.PLAY_SELECTED_TRACK_ID, 
                style: ButtonStyle.Primary, disabled: previousTracks.IsEmpty || selectedTrackName is null)
            .WithButton("Play now", customId: PlaybackHistoryModule.PLAY_SELECTED_TRACK_NOW_ID, 
                style: ButtonStyle.Primary, disabled: previousTracks.IsEmpty || selectedTrackName is null));
        
        ComponentBuilder componentBuilder = new ComponentBuilder()
            .WithRows(rows);

        string messageContent;
        if (previousTracks.IsEmpty) {
            messageContent = "No playback history was found";
        } else {
            messageContent = selectedTrackName is not null
                ? $"Track [{selectedTrackName}]({selectedTrackUri}) was selected"
                : "";
        }

        MessageProperties properties = new MessageProperties {
            Components = componentBuilder.Build(),
            Content = messageContent,
        };

        if (!previousTracks.IsEmpty) {
            properties.Embed = embedBuilder.Build();
        }

        return properties;
    }

    private async Task<MessageProperties> GenerateMusicMessageAsync(string guildName, MusicPlayerMetadata playerMetadata)
    {
        int pageNumber = playerMetadata.PageNumber;
        
        MessageProperties properties = new MessageProperties();
        properties.Embed = null;
        properties.Components = null;
        
        Color embedColor;
        string embedTitle;
        switch (playerMetadata.State) {
            case MusicPlayerState.Stopped:
                embedColor = EmbedColors.Error;   
                embedTitle = $"No party in {guildName}";
                break;

            case MusicPlayerState.Paused:
                embedColor = EmbedColors.Paused;
                embedTitle = $"DJ went out for a smoke break in {guildName}";
                break;

            case MusicPlayerState.Playing:
                embedColor = EmbedColors.OK;
                embedTitle = $"Dungeon party is started in {guildName}";
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        playerMetadata.Queue.TryPeek(out AudioQueueRecord? firstRecord);
        
        string nextSongsList = playerMetadata.Queue.Count > 1 ? "📋 **Next songs:**\n" : "";
        for (int i = 1 + (pageNumber - 1) * 10; i < 11 + (pageNumber - 1) * 10 && i < playerMetadata.Queue.Count; i++) {
            AudioQueueRecord record = playerMetadata.Queue.ElementAt(i);
            
            string title;
            if (record.PublicUrl is not null) {
                title = $"[{record.Author} - {record.Title}]({record.PublicUrl})";
            } else {
                title = $"{record.Author} - {record.Title}";
            }
            
            if (i < 10) {
                nextSongsList += $"`[{i}]`‎ ‎‏‏‎‎ ‎ ‎‏‏‎‎ {title}\n";
            } else {
                nextSongsList += $"`[{i}]`‎ ‎‏‏‎‎ {title}\n";
            }
        }

        string description;
        EmbedAuthorBuilder? authorBuilder = null;
        if (playerMetadata.Queue.IsEmpty) {
            description = "```" +
                          "Queue is empty for now\n" +
                          "You can go and fist your friend\n" +
                          "or play something with /play\n" +
                          "```";
        } else {
            if (firstRecord is null) {
                throw new Exception("Attempt to fetch first record from a non-empty queue was failed");
            }

            authorBuilder = new EmbedAuthorBuilder()
                .WithName($"Now playing: {firstRecord.Author} - {firstRecord.Title}")
                .WithIconUrl(firstRecord.Provider.Value.LogoUri);

            if (firstRecord.PublicUrl is not null) {
                authorBuilder.WithUrl(firstRecord.PublicUrl);
            }
            
            TimeSpan elapsed = playerMetadata.Elapsed;
            TimeSpan total = firstRecord.Duration;

            int barsProgressed = (int) Math.Floor(elapsed.TotalSeconds * ProgressBarsCount / total.TotalSeconds);
            if (barsProgressed > ProgressBarsCount) {
                barsProgressed = ProgressBarsCount;
            }

            StringBuilder playerBarsBuilder = new StringBuilder();
            if (barsProgressed == 0) {
                playerBarsBuilder.Append(Emojis.PLAYER_BAR_BG_LEFT_CORNER);
                for (int i = 0; i < ProgressBarsCount - 2; i++) {
                    playerBarsBuilder.Append(Emojis.PLAYER_BAR_BG);
                }

                playerBarsBuilder.Append(Emojis.PLAYER_BAR_BG_RIGHT_CORNER);
            } else if (barsProgressed == ProgressBarsCount) {
                playerBarsBuilder.Append(Emojis.PLAYER_BAR_SOLID_LEFT_CORNER);
                for (int i = 0; i < ProgressBarsCount - 2; i++) {
                    playerBarsBuilder.Append(Emojis.PLAYER_BAR_SOLID);
                }

                playerBarsBuilder.Append(Emojis.PLAYER_BAR_SOLID_RIGHT_CORNER);
            } else if (barsProgressed == 1) {
                playerBarsBuilder.Append(Emojis.PLAYER_BAR_SOLID_RIGHT_LEFT_CORNER);
                for (int i = 0; i < ProgressBarsCount - 2; i++) {
                    playerBarsBuilder.Append(Emojis.PLAYER_BAR_BG);
                }

                playerBarsBuilder.Append(Emojis.PLAYER_BAR_BG_RIGHT_CORNER);
            } else if (barsProgressed == 2) {
                playerBarsBuilder.Append(Emojis.PLAYER_BAR_SOLID_LEFT_CORNER);
                playerBarsBuilder.Append(Emojis.PLAYER_BAR_SOLID_BG_RIGHT_CORNER);
                for (int i = 0; i < ProgressBarsCount - 3; i++) {
                    playerBarsBuilder.Append(Emojis.PLAYER_BAR_BG);
                }

                playerBarsBuilder.Append(Emojis.PLAYER_BAR_BG_RIGHT_CORNER);
            } else {
                playerBarsBuilder.Append(Emojis.PLAYER_BAR_SOLID_LEFT_CORNER);
                for (int i = 0; i < barsProgressed - 2; i++) {
                    playerBarsBuilder.Append(Emojis.PLAYER_BAR_SOLID);
                }

                playerBarsBuilder.Append(Emojis.PLAYER_BAR_SOLID_BG_RIGHT_CORNER);
                for (int i = 0; i < ProgressBarsCount - barsProgressed - 1; i++) {
                    playerBarsBuilder.Append(Emojis.PLAYER_BAR_BG);
                }

                playerBarsBuilder.Append(Emojis.PLAYER_BAR_BG_RIGHT_CORNER);
            }

            description =
                $"{elapsed:mm\\:ss} ‎ ‎‏‏‎‎ " +
                $"{playerBarsBuilder} ‎ ‎‏‏‎‎ " +
                $"{total:mm\\:ss}\n\n" +
                nextSongsList;

        }

        int pagesCount = (int) Math.Ceiling((playerMetadata.Queue.Count - 1) / 10.0);
        if (pagesCount < 1) {
            pagesCount = 1;
        }

        string thumbnailUrl = "http://larc.tech/content/dungeon-bot/dj.png";
        if (firstRecord is not null) {
            thumbnailUrl = await firstRecord.AudioThumbnailUrl.Value ?? thumbnailUrl;
        }
        
        
        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithColor(embedColor)
            .WithTimestamp(DateTimeOffset.Now)
            .WithTitle(embedTitle)
            .WithFooter($"Page {pageNumber}/{pagesCount}‏‏‎ ‎‏‏‎‎ • ‏‏‎‏‏‎ {playerMetadata.Queue.Count} songs",
                "http://larc.tech/content/dungeon-bot/up-and-down.png")
            .WithThumbnailUrl(thumbnailUrl)
            .WithDescription(description);

        if (authorBuilder is not null) {
            embedBuilder.WithAuthor(authorBuilder);
        }
        
        ComponentBuilder componentBuilder = new ComponentBuilder();
        componentBuilder
            .WithRows(new[] {
                new ActionRowBuilder {
                    Components = new List<IMessageComponent> {
                        new ButtonBuilder {
                            Style = ButtonStyle.Primary,
                            Label = "‎‏‏‎‎‏‏‎ ‎‏‏‎‏‏‎ 🏠‏‎‎‏‏‎ ‎",
                            CustomId = QueueButtonHandler.QUEUE_HOME_PAGE_BUTTON_ID,
                            IsDisabled = pageNumber == 1
                        }.Build(),
                        new ButtonBuilder {
                            Style = ButtonStyle.Primary,
                            Label = "‎‏‏‎ ‎‎‏‏‎ ‎🢠‎‏‏‎ ‎‎‏‏‎ ‎",
                            CustomId = QueueButtonHandler.QUEUE_PREV_PAGE_BUTTON_ID,
                            IsDisabled = pageNumber <= 1
                        }.Build(),
                        new ButtonBuilder {
                            Style = ButtonStyle.Primary,
                            Label = "‎‏‏‎ ‎‎‏‏‎ ‎🢡‎‏‏‎ ‎‎‏‏‎ ‎",
                            CustomId = QueueButtonHandler.QUEUE_NEXT_PAGE_BUTTON_ID,
                            IsDisabled = pageNumber >= pagesCount
                        }.Build()
                    }
                },
                new ActionRowBuilder {
                    Components = new List<IMessageComponent> {
                        new ButtonBuilder {
                            Style = ButtonStyle.Primary,
                            Label = "‏‏‎ ‎‏‏‎ ‎❮❮‏‏‏‏‏‎ ‎‏‏‎ ‎",
                            IsDisabled = playerMetadata.PreviousTracks.IsEmpty,
                            CustomId = QueueButtonHandler.QUEUE_PREV_SONG_BUTTON_ID,
                        }.Build(),
                        new ButtonBuilder {
                            Style = playerMetadata.State switch
                            {
                                MusicPlayerState.Stopped => ButtonStyle.Success,
                                MusicPlayerState.Paused => ButtonStyle.Success,
                                MusicPlayerState.Playing => ButtonStyle.Secondary,
                                _ => throw new ArgumentOutOfRangeException()
                            } ,
                            Label = playerMetadata.State switch
                            {
                                MusicPlayerState.Stopped => "‎‏‏‎ ‎‎‏‏‎  ‎➤ ‎‎‏‏ ‎  ",
                                MusicPlayerState.Paused => "‎‏‏‎ ‎‎‏‏‎  ‎➤ ‎‎‏‏ ‎  ",
                                MusicPlayerState.Playing => "‎‏‏‎ ‎‎‏‏‎ ‎❚❚‎‏‏‎ ‎‎‏‏‎ ‎",
                                _ => throw new ArgumentOutOfRangeException()
                            } ,
                            IsDisabled = playerMetadata.Queue.IsEmpty,
                            CustomId = QueueButtonHandler.QUEUE_TOGGLE_STATE_BUTTON_ID,
                        }.Build(),
                        new ButtonBuilder {
                            Style = ButtonStyle.Primary,
                            Label = "‏‏‏‏‎ ‎‏‏‎ ‎❯❯‏‏‏‏‏‎ ‎‏‏‎ ‎",
                            IsDisabled = playerMetadata.Queue.Count < 2,
                            CustomId = QueueButtonHandler.QUEUE_NEXT_SONG_BUTTON_ID,
                        }.Build(),
                    }
                },
                new ActionRowBuilder {
                    Components = new List<IMessageComponent> {
                        new ButtonBuilder {
                            Style = playerMetadata.RepeatMode == RepeatMode.NoRepeat 
                                ? ButtonStyle.Primary
                                : ButtonStyle.Secondary,
                            Label = "No repeat",
                            CustomId = QueueButtonHandler.QUEUE_NO_REPEAT_BUTTON_ID,
                        }.Build(),
                        new ButtonBuilder {
                            Style = playerMetadata.RepeatMode == RepeatMode.RepeatSong 
                                ? ButtonStyle.Primary
                                : ButtonStyle.Secondary,
                            Label = "↻ ‏‏‎ ‎ Song",
                            CustomId = QueueButtonHandler.QUEUE_REPEAT_SONG_BUTTON_ID,
                        }.Build(),
                        new ButtonBuilder {
                            Style = playerMetadata.RepeatMode == RepeatMode.RepeatQueue
                                ? ButtonStyle.Primary
                                : ButtonStyle.Secondary,
                            Label = "↻ ‏‏‎ ‎ Queue",
                            CustomId = QueueButtonHandler.QUEUE_REPEAT_QUEUE_BUTTON_ID,
                        }.Build(),
                    }
                },
                new ActionRowBuilder {
                    Components = new List<IMessageComponent> {
                        new ButtonBuilder {
                            Style = ButtonStyle.Primary,
                            Label = "↝‏‏‎ ‎‏‏‎ ‎ Shuffle queue",
                            IsDisabled = playerMetadata.Queue.IsEmpty,
                            CustomId = QueueButtonHandler.QUEUE_SHUFFLE_BUTTON_ID,
                        }.Build(),
                        new ButtonBuilder {
                            Style = ButtonStyle.Danger,
                            Label = "Clear queue‏‏‎ ‎",
                            Emote = new Emoji("🗑️"),
                            IsDisabled = playerMetadata.Queue.IsEmpty,
                            CustomId = QueueButtonHandler.QUEUE_CLEAR_QUEUE_BUTTON_ID,
                        }.Build(),
                    }
                },
            });

        properties.Embed = embedBuilder.Build();
        properties.Components = componentBuilder.Build();

        return properties;
    }

    public MessageProperties GenerateMissingPermissionsMessage(
        string description,
        ChannelPermission[] requiredPermissions,
        SocketGuildChannel channel)
    {
        ChannelPermissions channelPermissions = channel.Guild.CurrentUser.GetPermissions(channel);
        StringBuilder embedDescriptionBuilder = new StringBuilder();
        foreach (ChannelPermission requiredPermission in requiredPermissions) {
            embedDescriptionBuilder.Append(channelPermissions.Has(requiredPermission) ? Emojis.CORRECT : Emojis.INCORRECT);
            embedDescriptionBuilder.Append($" **{Enum.GetName(requiredPermission).SplitCamelCase().ToLower().FirstCharToUpperCase()}**");
            embedDescriptionBuilder.AppendLine();
        }

        MessageProperties properties = new MessageProperties();
        properties.Embed = new EmbedBuilder()
            .WithColor(EmbedColors.Error)
            .WithAuthor(channel.Guild.CurrentUser)
            .WithCurrentTimestamp()
            .WithTitle(description)
            .WithDescription(embedDescriptionBuilder.ToString())
            .Build();

        return properties;
    }

    public MessageProperties GenerateMusicServiceNotFoundMessage(IUser botUser, string userQuery)
    {
        StringBuilder descriptionBuilder = new StringBuilder();
        descriptionBuilder.AppendLine("It seems like the audio source points to the unsupported music service.");
        descriptionBuilder.AppendLine($"Bot received: {userQuery}");
        descriptionBuilder.AppendLine();
        descriptionBuilder.AppendLine("**List of the supported music services:**");

        foreach (MusicProvider provider in MusicProvider.List.OrderBy(p => p.Value.DisplayName.Length)) {
            BaseMusicProviderController controller = provider.Value;
            descriptionBuilder.AppendLine($"{controller.LogoEmojiId} ‎ ‎‏‏‎‎**{controller.DisplayName} — " +
                                          $"https://{controller.LinksDomainName}/**");
        }
        
        MessageProperties properties = new MessageProperties();
        properties.Embed = new EmbedBuilder()
            .WithColor(EmbedColors.Error)
            .WithAuthor(botUser)
            .WithCurrentTimestamp()
            .WithTitle("Bot is unable to parse the query parameter")
            .WithDescription(descriptionBuilder.ToString())
            .Build();

        return properties;
    }
    
    public MessageProperties GenerateMusicServiceLinkNotSupportedMessage(BaseMusicProviderController providerControllerUsed, string userQuery)
    {
        StringBuilder descriptionBuilder = new StringBuilder();
        descriptionBuilder.AppendLine("It seems like the music service couldn't handle this link");
        descriptionBuilder.AppendLine($"Bot received: {userQuery}");
        descriptionBuilder.AppendLine();
        descriptionBuilder.AppendLine($"**What links {providerControllerUsed.DisplayName} can handle:**");
        descriptionBuilder.AppendLine(providerControllerUsed.SupportedLinks);
        
        MessageProperties properties = new MessageProperties();
        properties.Embed = new EmbedBuilder()
            .WithColor(EmbedColors.Error)
            .WithAuthor(new EmbedAuthorBuilder {
                Name = "Bot is unable to parse the query parameter",
                IconUrl = providerControllerUsed.LogoUri
            })
            .WithCurrentTimestamp()
            .WithDescription(descriptionBuilder.ToString())
            .Build();

        return properties;
    }
    
    public MessageProperties GenerateNewUserMessage(IUser botUser, IUser joinedUser)
    {
        MessageProperties properties = new MessageProperties();
        properties.Embed = new EmbedBuilder()
            .WithColor(EmbedColors.Info)
            .WithImageUrl("https://cdn-longterm.mee6.xyz/plugins/welcome/images/1021747104605016167/c37a38c3a314295a9ae91a51bab03aac4b1d22a54ffe15ba8512b0f891eb359f.gif")
            .WithTitle("Welcome to the GYM")
            .WithAuthor(botUser)
            .WithCurrentTimestamp()
            .WithDescription($"New fucking slave <@{joinedUser.Id}> joined the gym!")
            .Build();

        return properties;
    }
    
    public MessageProperties GenerateLeftUserMessage(IUser botUser, IUser leftUser)
    {
        MessageProperties properties = new MessageProperties();
        properties.Embed = new EmbedBuilder()
            .WithColor(EmbedColors.Info)
            .WithAuthor(botUser)
            .WithCurrentTimestamp()
            .WithTitle($"{leftUser.Username} позорно сбежал из данжа")
            .Build();

        return properties;
    }

    private static class EmbedColors
    {
        public static readonly Color OK = new Color(14, 189, 17);
        public static readonly Color Error = new Color(220, 16, 71);
        public static readonly Color Paused = new Color(235, 173, 15);
        public static readonly Color Info = new Color(17, 227, 227);
    }
}