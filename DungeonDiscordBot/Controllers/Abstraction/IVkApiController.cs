using System.Threading.Tasks;

namespace DungeonDiscordBot.Controllers.Abstraction
{
    public interface IVkApiController
    {
        Task Init(IServicesAggregator aggregator);
    }
}