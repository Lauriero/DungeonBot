﻿using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.Settings;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DungeonDiscordBot.Services;

public class BotDataContext : DbContext
{
    public BotDataContext(IOptions<AppSettings> appSettings) : 
        base(new DbContextOptionsBuilder<BotDataContext>()
                .UseMySql(appSettings.Value.DBConnectionString, 
                    ServerVersion.AutoDetect(appSettings.Value.DBConnectionString))
                .Options) { }
    
    public DbSet<Guild> Guilds { get; set; } = null!;

    public DbSet<MusicQueryHistoryEntity> MusicQueries { get; set; } = null!;

    public DbSet<FavoriteMusicCollection> FavoriteCollections { get; set; } = null!;
}