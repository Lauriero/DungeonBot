using System.Threading.Tasks;

namespace DungeonDiscordBot.Controllers.Abstraction
{
    public interface IDiscordBotController
    {
        Task Init(IServicesAggregator aggregator);

        Task HandleInteractionException(Exception exception);
    }
}