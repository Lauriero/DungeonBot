﻿using System;
using System.Threading;
using System.Threading.Tasks;

using Discord.WebSocket;

namespace DungeonDiscordBot.Controllers.Abstraction
{
    public interface IDiscordBotService : IRequireInitiationService
    {
        Task HandleInteractionException(Exception exception);

        Task<SocketTextChannel> GetChannelAsync(ulong channelId, CancellationToken token = default);
    }
}