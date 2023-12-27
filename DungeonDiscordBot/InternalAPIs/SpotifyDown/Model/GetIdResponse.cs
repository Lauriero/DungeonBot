using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json;

namespace DungeonDiscordBot.InternalAPIs.SpotifyDown.Model;

/// <summary>
/// Response of the "https://api.spotifydown.com/getId/:id" method.
/// Contains YouTube video ID or the error message.
/// </summary>
[Serializable]
public class GetIdResponse
{
    /// <summary>
    /// Contains false when the request was completed with an error, true otherwise.
    /// If the request was completed successfully, <see cref="Id"/> will be defined.
    /// If the request was completed with an error, <see cref="Message"/> will be defined.
    /// </summary>
    [JsonProperty("success")]
    [MemberNotNullWhen(true, nameof(Id))]
    [MemberNotNullWhen(false, nameof(Message))]
    public bool Success { get; set; }

    /// <summary>
    /// Description of the error, if one has occurred.
    /// </summary>
    [JsonProperty("message")]
    public string? Message { get; set; }
    
    /// <summary>
    /// String ID of the youtube video,
    /// corresponding to the requested spotify track ID.
    /// </summary>
    [JsonProperty("id")]
    public string? Id { get; set; }
}