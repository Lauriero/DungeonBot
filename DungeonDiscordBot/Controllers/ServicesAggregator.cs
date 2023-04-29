using System;
using System.Threading.Tasks;

using DungeonDiscordBot.Controllers.Abstraction;

namespace DungeonDiscordBot.Controllers
{
    public class ServicesAggregator : IServicesAggregator
    {
        public IDiscordAudioController DiscordAudio { get; }
        
        public IDiscordBotController DiscordBot { get; }
        
        public IVkApiController VkApi { get; }

        public IServiceProvider ServiceProvider { get; private set; } = null!;

        public ServicesAggregator(IDiscordAudioController discordAudio, IDiscordBotController discordBot, IVkApiController vkApi)
        {
            VkApi      = vkApi;
            DiscordBot = discordBot;
            DiscordAudio = discordAudio;
        }
        
        public async Task Init(IServiceProvider provider)
        {
            ServiceProvider = provider;
            await DiscordAudio.Init(this);
            await DiscordBot.Init(this);
            await VkApi.Init(this);
        }
    }
}