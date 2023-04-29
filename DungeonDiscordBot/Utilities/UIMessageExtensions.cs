using System;
using System.Collections.Concurrent;
using System.Linq;

using Discord;

using DungeonDiscordBot.ButtonHandlers;
using DungeonDiscordBot.Model;

namespace DungeonDiscordBot.Utilities;

public static class UIMessageExtensions
{
    public static void GenerateQueueMessage(this MessageProperties originalMessage,
        ConcurrentQueue<AudioQueueRecord> queue, int pageNumber = 1)
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

        int pagesCount = (int) Math.Ceiling((queue.Count - 1) / 10.0);
        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithFooter($"Page {pageNumber}/{pagesCount} - {queue.Count} entries")
            .WithThumbnailUrl(firstRecord.AudioThumbnailUrl ?? "http://larc.tech/content/dungeon-bot/music.png");

        EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder()
            .WithName("Next");

        for (int i = 1 + (pageNumber - 1) * 10; i < 11 + (pageNumber - 1) * 10; i++) {
            AudioQueueRecord record = queue.ElementAt(i);
            fieldBuilder.Value += $"`[{i + 1}] {record.Author} - {record.Title}`\n";
        }

        embedBuilder.AddField("Current", $"{firstRecord.Author} - {firstRecord.Title}");
        embedBuilder.AddField(fieldBuilder);

        ComponentBuilder componentBuilder = new ComponentBuilder();

        if (pageNumber > 1) {
            componentBuilder.WithButton("🠴",
                style: ButtonStyle.Primary,
                customId: $"{QueueButtonHandler.QUEUE_PAGE_BUTTON_ID_PREFIX}-{pageNumber - 1}");
        }

        if (pageNumber < pagesCount) {
            componentBuilder.WithButton("🠶",
                style: ButtonStyle.Primary,
                customId: $"{QueueButtonHandler.QUEUE_PAGE_BUTTON_ID_PREFIX}-{pageNumber + 1}");
        }

        originalMessage.Embed = embedBuilder.Build();
        originalMessage.Components = componentBuilder.Build();
    }
}