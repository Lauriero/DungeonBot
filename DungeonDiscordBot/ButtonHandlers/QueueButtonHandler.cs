using System;
using System.Threading.Tasks;

using Discord.WebSocket;

using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Utilities;

namespace DungeonDiscordBot.ButtonHandlers;

public class QueueButtonHandler : IButtonHandler
{
    public const string QUEUE_PAGE_BUTTON_ID_PREFIX = "queue-page";

    public string Prefix => "queue";

    private IServicesAggregator _aggregator;
    
    public QueueButtonHandler(IServicesAggregator aggregator)
    {
        _aggregator = aggregator;
    }
    
    public async Task OnButtonExecuted(SocketMessageComponent component)
    {
        await component.DeferAsync();
        if (component.Data.CustomId.StartsWith(QUEUE_PAGE_BUTTON_ID_PREFIX)) {
            int pageNumber = Convert.ToInt32(component.Data.CustomId[
                (QUEUE_PAGE_BUTTON_ID_PREFIX.Length + 1)..
            ]);

            await component.Message.ModifyAsync((m) =>
                m.GenerateQueueMessage(_aggregator.DiscordAudio.GetQueue((ulong) component.GuildId!), pageNumber));
            await component.RespondAsync();
        }
    }
}
