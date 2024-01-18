namespace DungeonDiscordBot.Settings;

public class ProxySettings
{
    public const string OPTIONS_SECTION_NAME = nameof(ProxySettings);

    public bool UseProxy { get; set; } = false;

    public string ProxyUrl { get; set; } = null!;
    
    public string? Username { get; set; }
    
    public string? Password { get; set; }
}