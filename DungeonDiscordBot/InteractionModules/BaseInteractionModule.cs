using Discord;
using Discord.Interactions;

using Microsoft.Extensions.Logging;

namespace DungeonDiscordBot.InteractionModules;

public class BaseInteractionModule<TContext> : InteractionModuleBase<TContext>
    where TContext : class, IInteractionContext
{
    private readonly ILogger<BaseInteractionModule<TContext>> _logger;

    public BaseInteractionModule(ILogger<BaseInteractionModule<TContext>> logger)
    {
        _logger = logger;
    }
    
    protected async Task MethodWrapper(Func<Task> inner, bool deleteAfter = true)
    {
        try {
            await inner();
            if (deleteAfter) {
                await Task.Delay(15000);
                await DeleteOriginalResponseAsync();
            }
        } catch (Exception e) {
            _logger.LogError(e, "Interaction was executed with an exception");
        }
    }    
}