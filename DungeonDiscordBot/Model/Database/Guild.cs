using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DungeonDiscordBot.Model.Database;

[Table("guild")]
public class Guild
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Required]
    [Column("name")]
    [StringLength(maximumLength: 255)]
    public string Name { get; set; } = null!;

    [Column("music_channel_id")]
    public ulong? MusicChannelId { get; set; }

    [Column("music_message_id")]
    public ulong? MusicMessageId { get; set; }
    
    [Column("welcome_channel_id")]
    public ulong? WelcomeChannelId { get; set; }
    
    [Column("runaway_channel_id")]
    public ulong? RunawayChannelId { get; set; }
    
    public ICollection<MusicQueryHistoryEntity> History { get; set; } = null!;
}