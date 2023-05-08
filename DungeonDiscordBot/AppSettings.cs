﻿namespace DungeonDiscordBot;

    public class AppSettings
{
    public const string OPTIONS_SECTION_NAME = nameof(AppSettings); 
    
    public string VKLogin { get; set; } = String.Empty;
    public string VKPassword { get; set; } = String.Empty;
    public string DiscordBotToken { get; set; } = String.Empty;
    
    public string DBConnectionString { get; set; } = String.Empty;
}