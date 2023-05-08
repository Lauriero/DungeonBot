using System.Collections.Concurrent;

using Discord;

using DungeonDiscordBot.ButtonHandlers;
using DungeonDiscordBot.Model;

namespace DungeonDiscordBot.Utilities;

public static class UIMessageExtensions
{
    public static void GenerateQueueMessage(this MessageProperties originalMessage,
        ConcurrentQueue<AudioQueueRecord> queue, string guildName, int pageNumber = 1)
    {
        if (queue.IsEmpty) {
            originalMessage.Content = "Queue is empty";
            originalMessage.Embed = null;
            originalMessage.Components = null;
            return;
        }
        
        if (!queue.TryPeek(out AudioQueueRecord? firstRecord)) {
            throw new Exception();
        }

        string nextSongsList = "";
        for (int i = 1 + (pageNumber - 1) * 10; i < 11 + (pageNumber - 1) * 10; i++) {
            AudioQueueRecord record = queue.ElementAt(i);
            if (i < 10) {
                nextSongsList += $"`[{i}]`‎ ‎‏‏‎‎ ‎ ‎‏‏‎‎ {record.Author} - {record.Title}\n";
            } else {
                nextSongsList += $"`[{i}]`‎ ‎‏‏‎‎ {record.Author} - {record.Title}\n";
            }
        }
        
        int pagesCount = (int) Math.Ceiling((queue.Count - 1) / 10.0);
        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithColor(14, 189, 17)
            .WithTimestamp(DateTimeOffset.Now)
            .WithTitle($"Dungeon party is started in {guildName}")
            .WithFooter($"Page {pageNumber}/{pagesCount}‏‏‎ ‎‏‏‎‎ • ‏‏‎‏‏‎ {queue.Count} songs",
                "http://larc.tech/content/dungeon-bot/up-and-down.png")
            .WithThumbnailUrl(firstRecord.AudioThumbnailUrl ?? "http://larc.tech/content/dungeon-bot/dj.png")
            .WithDescription(
                $"🎶 **Now playing:**  ***[{firstRecord.Author} - {firstRecord.Title}](https://google.com/)***\n" +
                $"00:23 ‎ ‎‏‏‎‎ [▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰](https://google.com/)▱▱ ‎ ‎‏‏‎‎ 08:30\n\n" +
                $"📋 **Next songs:**\n" + nextSongsList);


        ComponentBuilder componentBuilder = new ComponentBuilder();
        componentBuilder
            .WithRows(new[] {
                new ActionRowBuilder {
                    Components = new List<IMessageComponent> {
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
                            IsDisabled = pageNumber > pagesCount
                        }.Build()
                    }
                },
                new ActionRowBuilder {
                    Components = new List<IMessageComponent> {
                        new ButtonBuilder {
                            Style = ButtonStyle.Primary,
                            Label = "‏‏‎ ‎‏‏‎ ‎❮❮‏‏‏‏‏‎ ‎‏‏‎ ‎",
                            CustomId = $"next",
                        }.Build(),
                        new ButtonBuilder {
                            Style = ButtonStyle.Secondary,
                            Label = "‎‏‏‎ ‎‎‏‏‎ ‎❚❚‎‏‏‎ ‎‎‏‏‎ ‎",
                            CustomId = $"toggle",
                        }.Build(),
                        new ButtonBuilder {
                            Style = ButtonStyle.Primary,
                            Label = "‏‏‏‏‎ ‎‏‏‎ ‎❯❯‏‏‏‏‏‎ ‎‏‏‎ ‎",
                            CustomId = $"prev",
                        }.Build(),
                    }
                },
                new ActionRowBuilder {
                    Components = new List<IMessageComponent> {
                        new ButtonBuilder {
                            Style = ButtonStyle.Primary,
                            Label = "No repeat",
                            CustomId = $"no-repeat",
                        }.Build(),
                        new ButtonBuilder {
                            Style = ButtonStyle.Secondary,
                            Label = "↻ ‏‏‎ ‎ Song",
                            CustomId = $"repeat-song",
                        }.Build(),
                        new ButtonBuilder {
                            Style = ButtonStyle.Secondary,
                            Label = "↻ ‏‏‎ ‎ Queue",
                            CustomId = $"repeat-queue",
                        }.Build(),
                    }
                },
                new ActionRowBuilder {
                    Components = new List<IMessageComponent> {
                        new ButtonBuilder {
                            Style = ButtonStyle.Primary,
                            Label = "↝‏‏‎ ‎‏‏‎ ‎ Shuffle queue",
                            CustomId = $"shuffle",
                        }.Build(),
                        new ButtonBuilder {
                            Style = ButtonStyle.Danger,
                            Label = "Clear queue‏‏‎ ‎",
                            CustomId = $"clear",
                            Emote = new Emoji("🗑️")
                        }.Build(),
                    }
                },
            });

        originalMessage.Embed = embedBuilder.Build();
        originalMessage.Components = componentBuilder.Build();
    }
}