using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

using DungeonDiscordBot.ButtonHandlers;
using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Exceptions;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;

using Microsoft.Extensions.DependencyInjection;

namespace DungeonDiscordBot.Controllers;

public class UserInterfaceService : IUserInterfaceService
{
    private const int PROGRESS_BARS_COUNT = 20;
    
    public int InitializationPriority => 6;
    
    private IDiscordBotService _botService = null!;
    private readonly ISettingsService _settingsService;
    private readonly IServiceProvider _serviceProvider;

    public UserInterfaceService(ISettingsService settingsService, IServiceProvider serviceProvider)
    {
        _settingsService = settingsService;
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync()
    {
        _botService = _serviceProvider.GetRequiredService<IDiscordBotService>();
    }

    public async Task CreateSongsQueueMessageAsync(ulong guildId, 
        ConcurrentQueue<AudioQueueRecord> queue, MusicPlayerMetadata playerMetadata,
        SocketTextChannel musicChannel, CancellationToken token = default)
    {
        Guild guild = await _settingsService.GetGuildDataAsync(guildId, token);
        
        MessageProperties musicMessage = await GenerateMessageAsync(guild.Name, queue, playerMetadata);
        RestUserMessage message = await musicChannel.SendMessageAsync("", 
            embed: musicMessage.Embed.Value, components: musicMessage.Components.Value);

        await _settingsService.RegisterMusicChannel(guildId, musicChannel.Id, message.Id, token);
    }
    
    /// <summary>
    /// Updates the current message that is used to control music.
    /// </summary>
    public async Task UpdateSongsQueueMessageAsync(ulong guildId, 
        ConcurrentQueue<AudioQueueRecord> queue, MusicPlayerMetadata playerMetadata, 
        string message = "", CancellationToken token = default)
    {
        Guild guild = await _settingsService.GetGuildDataAsync(guildId, token);
        MessageProperties musicMessage = await GenerateMessageAsync(guild.Name, queue, playerMetadata);

        if (guild.MusicChannelId is null || guild.MusicMessageId is null) {
            throw new MusicChannelNotRegisteredException();
        }

        SocketTextChannel textChannel = await _botService.GetChannelAsync(guild.MusicChannelId.Value, token);
        await textChannel.ModifyMessageAsync(guild.MusicMessageId.Value, m => {
            m.Content = string.IsNullOrEmpty(message) ? new Optional<string>() : message;
            m.Embed = musicMessage.Embed;
            m.Components = musicMessage.Components;
        }, new RequestOptions {CancelToken = token});
    }

    private async Task<MessageProperties> GenerateMessageAsync(string guildName, 
        ConcurrentQueue<AudioQueueRecord> queue, MusicPlayerMetadata playerMetadata)
    {
        int pageNumber = playerMetadata.PageNumber;
        
        MessageProperties properties = new MessageProperties();
        properties.Embed = null;
        properties.Components = null;
        
        Color embedColor;
        string embedTitle;
        switch (playerMetadata.State) {
            case MusicPlayerState.Stopped:
                embedColor = new Color(220, 16, 71);   
                embedTitle = $"No party in {guildName}";
                break;

            case MusicPlayerState.Paused:
                embedColor = new Color(235, 173, 15);
                embedTitle = $"DJ went out for a smoke break in {guildName}";
                break;

            case MusicPlayerState.Playing:
                embedColor = new Color(14, 189, 17);
                embedTitle = $"Dungeon party is started in {guildName}";
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
        
        queue.TryPeek(out AudioQueueRecord? firstRecord);

        string nextSongsList = queue.Count > 1 ? "📋 **Next songs:**\n" : "";
        for (int i = 1 + (pageNumber - 1) * 10; i < 11 + (pageNumber - 1) * 10 && i < queue.Count; i++) {
            AudioQueueRecord record = queue.ElementAt(i);
            if (i < 10) {
                nextSongsList += $"`[{i}]`‎ ‎‏‏‎‎ ‎ ‎‏‏‎‎ {record.Author} - {record.Title}\n";
            } else {
                nextSongsList += $"`[{i}]`‎ ‎‏‏‎‎ {record.Author} - {record.Title}\n";
            }
        }

        string description;
        if (queue.IsEmpty) {
            description = "```" +
                          "Queue is empty for now\n" +
                          "You can go and fist your friend\n" +
                          "or play something with /play\n" +
                          "```";
        } else if (firstRecord is not null) {
            TimeSpan elapsed = playerMetadata.Elapsed;
            TimeSpan total = await firstRecord.Duration;
            
            int barsProgressed = (int)Math.Floor(elapsed.TotalSeconds * PROGRESS_BARS_COUNT / total.TotalSeconds);
            description = $"🎶 **Now playing:**  ***[{firstRecord.Author} - {firstRecord.Title}](https://google.com/)***\n" +
                          $"{elapsed:mm\\:ss} ‎ ‎‏‏‎‎ " +
                          (barsProgressed > 0 ? $"[{new String('▰', barsProgressed)}](https://google.com/)" : "") +
                          $"{new String('▱', PROGRESS_BARS_COUNT - barsProgressed)} ‎ ‎‏‏‎‎ " +
                          $"{total:mm\\:ss}\n\n" +
                          nextSongsList;
        } else {
            throw new Exception();
        }
        
        int pagesCount = (int) Math.Ceiling((queue.Count - 1) / 10.0);
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
            .WithFooter($"Page {pageNumber}/{pagesCount}‏‏‎ ‎‏‏‎‎ • ‏‏‎‏‏‎ {queue.Count} songs",
                "http://larc.tech/content/dungeon-bot/up-and-down.png")
            .WithThumbnailUrl(thumbnailUrl)
            .WithDescription(description);
        
        ComponentBuilder componentBuilder = new ComponentBuilder();
        componentBuilder
            .WithRows(new[] {
                new ActionRowBuilder {
                    Components = new List<IMessageComponent> {
                        new ButtonBuilder {
                            Style = ButtonStyle.Primary,
                            Label = "‎‏‏‎‎‏‏‎ ‎‏‏‎‏‏‎ 🏠‏‎‎‏‏‎ ‎",
                            CustomId = $"{QueueButtonHandler.QUEUE_HOME_PAGE_BUTTON_ID}",
                            IsDisabled = pageNumber == 1
                        }.Build(),
                        new ButtonBuilder {
                            Style = ButtonStyle.Primary,
                            Label = "‎‏‏‎ ‎‎‏‏‎ ‎🢠‎‏‏‎ ‎‎‏‏‎ ‎",
                            CustomId = $"{QueueButtonHandler.QUEUE_PAGE_BUTTON_ID_PREFIX}-{pageNumber - 1}",
                            IsDisabled = pageNumber <= 1
                        }.Build(),
                        new ButtonBuilder {
                            Style = ButtonStyle.Primary,
                            Label = "‎‏‏‎ ‎‎‏‏‎ ‎🢡‎‏‏‎ ‎‎‏‏‎ ‎",
                            CustomId = $"{QueueButtonHandler.QUEUE_PAGE_BUTTON_ID_PREFIX}-{pageNumber + 1}",
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
                            IsDisabled = queue.IsEmpty,
                            CustomId = QueueButtonHandler.QUEUE_TOGGLE_STATE_BUTTON_ID,
                        }.Build(),
                        new ButtonBuilder {
                            Style = ButtonStyle.Primary,
                            Label = "‏‏‏‏‎ ‎‏‏‎ ‎❯❯‏‏‏‏‏‎ ‎‏‏‎ ‎",
                            IsDisabled = queue.Count < 2,
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
                            IsDisabled = queue.IsEmpty,
                            CustomId = QueueButtonHandler.QUEUE_SHUFFLE_BUTTON_ID,
                        }.Build(),
                        new ButtonBuilder {
                            Style = ButtonStyle.Danger,
                            Label = "Clear queue‏‏‎ ‎",
                            Emote = new Emoji("🗑️"),
                            IsDisabled = queue.IsEmpty,
                            CustomId = QueueButtonHandler.QUEUE_CLEAR_QUEUE_BUTTON_ID,
                        }.Build(),
                    }
                },
            });

        properties.Embed = embedBuilder.Build();
        properties.Components = componentBuilder.Build();

        return properties;
    }
}