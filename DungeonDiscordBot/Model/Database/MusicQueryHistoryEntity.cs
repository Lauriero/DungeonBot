using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DungeonDiscordBot.Model.Database;

[Table("music_query_history")]
public class MusicQueryHistoryEntity
{
    [Key]
    [Required]
    [Column("id")]
    public ulong Id { get; set; }

    [Required]
    [Column("guild_id")]
    [ForeignKey("Guild")]
    public ulong GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    [Required]
    [StringLength(255)]
    [Column("query_name")]
    public string QueryName { get; set; } = null!;

    [Required]
    [StringLength(255)]
    [Column("query_value")]
    public string QueryValue { get; set; } = null!;
    
    [Required]
    [Column("queried_at")]
    public DateTime QueriedAt { get; set; }
}