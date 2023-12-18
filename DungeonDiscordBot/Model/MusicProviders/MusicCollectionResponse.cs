using System.Diagnostics.CodeAnalysis;

namespace DungeonDiscordBot.Model.MusicProviders;

/// <summary>
/// Response of the music provider.
/// Contains music list with some metadata about the query.
/// If the audio collection has no entries, it is advised to set <see cref="Name"/> to "Not found".
/// </summary>
public class MusicCollectionResponse
{
    /// <summary>
    /// Specifies whether the response was successful.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Name))]
    [MemberNotNullWhen(true, nameof(ErrorType))]
    [MemberNotNullWhen(true, nameof(ErrorMessage))]
    public bool IsError { get; } 

    /// <summary>
    /// Provider that fetched the music for this collection.
    /// </summary>
    public MusicProvider Provider { get; }
    
    /// <summary>
    /// General name of the collection.
    /// May be the song full name (artist - song),
    /// album name (artist - album),
    /// playlist name, etc...
    /// Or null, if <see cref="IsError"/> is true. 
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Array of audios that were fetched.
    /// </summary>
    public IEnumerable<AudioQueueRecord> Audios { get; }
    
    /// <summary>
    /// Specifies the type of the error
    /// that has occurred during the attempt to retrieve audios. 
    /// </summary>
    public MusicResponseErrorType? ErrorType { get; }

    /// <summary>
    /// Specifies the details about the error
    /// that has occurred during the attempt to retrieve audios. 
    /// </summary>
    public string? ErrorMessage { get; }

    public static MusicCollectionResponse FromSuccess(MusicProvider provider, string name,
        IEnumerable<AudioQueueRecord> audios)
    {
        return new MusicCollectionResponse(provider, name, audios, null, null);
    }

    public static MusicCollectionResponse FromError(MusicProvider provider, MusicResponseErrorType errorType, string errorMessage)
    {
        return new MusicCollectionResponse(provider, null, Array.Empty<AudioQueueRecord>(), errorType, errorMessage);
    }

    private MusicCollectionResponse(MusicProvider provider, string? name, IEnumerable<AudioQueueRecord> audios,
        MusicResponseErrorType? errorType, string? errorMessage)
    {
        Provider = provider;
        Name = name;
        Audios = audios;
        IsError = errorType is not null;
        ErrorType = errorType;
        ErrorMessage = errorMessage;
    }
}