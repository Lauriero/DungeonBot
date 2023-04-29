using System;
using System.Threading.Tasks;

using Discord.Interactions;
using Discord.WebSocket;

using DungeonDiscordBot.Controllers;
using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

namespace DungeonDiscordBot
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            await new HostBuilder()
                .ConfigureServices((hostContext, services) => {
                    services.AddHostedService<BotHostedService>();
                }).RunConsoleAsync();
        }
    }
}