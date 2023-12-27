namespace DungeonDiscordBot.Settings;

public class AppSettings
{
    public const string OPTIONS_SECTION_NAME = nameof(AppSettings); 
    
    public string VKLogin { get; set; } = String.Empty;
    
    public string VKPassword { get; set; } = String.Empty;
    
    public string YMToken { get; set; } = String.Empty;
    
    public string SpotifyClientId { get; set; } = String.Empty;
    
    public string SpotifyClientSecret { get; set; } = String.Empty;
    
    public string DiscordBotToken { get; set; } = String.Empty;
    
    public string DBConnectionString { get; set; } = String.Empty;
    
    public string FFMpegExecutable { get; set; } = String.Empty;
}