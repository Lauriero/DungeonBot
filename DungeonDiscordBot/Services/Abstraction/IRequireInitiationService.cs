namespace DungeonDiscordBot.Services.Abstraction;

public interface IRequireInitiationService
{
    public int InitializationPriority { get; }
    
    /// <summary>
    /// Initializes the service.
    /// </summary>
    public Task InitializeAsync();
}