using System;
using System.Threading.Tasks;

namespace DungeonDiscordBot.Controllers.Abstraction
{
    public interface IServicesAggregator
    {
        IDiscordAudioController DiscordAudio { get; }

        IDiscordBotController DiscordBot { get; }
        
        IVkApiController VkApi { get; }
        
        IServiceProvider ServiceProvider { get; }

        Task Init(IServiceProvider provider);
    }
}