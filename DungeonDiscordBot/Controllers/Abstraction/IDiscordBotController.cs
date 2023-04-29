using System.Threading.Tasks;

namespace DungeonDiscordBot.Controllers.Abstraction
{
    public interface IDiscordBotController
    {
        Task Init(IServicesAggregator aggregator);
    }
}