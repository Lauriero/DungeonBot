using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace DungeonDiscordBot.Model.Database;

/// <summary>
/// Represents the collection that user has added to the personal favoriteCollections.
/// </summary>
[Table("favorite_music_collection")]
public class FavoriteMusicCollection
{
    [Key]
    [Required]
    [Column("id")]
    public ulong Id { get; set; }
    
    /// <summary>
    /// Id of the discord user, that added this collection to favoriteCollections.
    /// </summary>
    [Required]
    [Column("user_id")]
    public ulong UserId { get; set; }

    /// <summary>
    /// Name of the music collection.
    /// </summary>
    [Required]
    [Column("collection_name")]
    public string CollectionName { get; set; } = null!;
    
    /// <summary>
    /// Url that is used to fetch audios of the collection.
    /// </summary>
    [Required]
    [Column("query")]
    public string Query { get; set; } = null!;
    
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = default!;
}