using DungeonDiscordBot.Model.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DungeonDiscordBot.Controllers;

public class BotDataContext : DbContext
{
    public BotDataContext(IOptions<AppSettings> appSettings) : 
        base(new DbContextOptionsBuilder<BotDataContext>()
                .UseMySql(appSettings.Value.DBConnectionString, 
                    ServerVersion.AutoDetect(appSettings.Value.DBConnectionString))
                .Options) { }
    
    public DbSet<Guild> Guilds { get; set; } = null!;
}