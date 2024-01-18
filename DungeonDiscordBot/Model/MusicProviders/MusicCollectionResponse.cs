using System.Diagnostics.CodeAnalysis;

namespace DungeonDiscordBot.Model.MusicProviders;

/// <summary>
/// Collection of the music provider.
/// Contains music list with some metadata about the query.
/// If the audio collection has no entries, it is advised to set <see cref="Name"/> to "Not found".
/// </summary>
public class MusicCollectionResponse
{
    /// <summary>
    /// Specifies whether the response was successful.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Metadata))]
    [MemberNotNullWhen(true, nameof(ErrorType))]
    [MemberNotNullWhen(true, nameof(ErrorMessage))]
    public bool IsError { get; } 

    /// <summary>
    /// Provider that fetched the music for this collection.
    /// </summary>
    public MusicProvider Provider { get; }
    
    /// <summary>
    /// Contains fetched collection data.
    /// </summary>
    public MusicCollectionMetadata? Metadata { get; }

    /// <summary>
    /// Array of audios that were fetched.
    /// </summary>
    public IList<AudioQueueRecord> Audios { get; }
    
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

    public static MusicCollectionResponse FromSuccess(MusicProvider provider, MusicCollectionMetadata? metadata,
        IList<AudioQueueRecord> audios)
    {
        return new MusicCollectionResponse(provider, metadata, audios, null, null);
    }

    public static MusicCollectionResponse FromError(MusicProvider provider, MusicResponseErrorType errorType, string errorMessage)
    {
        return new MusicCollectionResponse(provider, null, Array.Empty<AudioQueueRecord>(), errorType, errorMessage);
    }

    private MusicCollectionResponse(MusicProvider provider, MusicCollectionMetadata? metadata, IList<AudioQueueRecord> audios,
        MusicResponseErrorType? errorType, string? errorMessage)
    {
        Provider = provider;
        Metadata = metadata;
        Audios = audios;
        IsError = errorType is not null;
        ErrorType = errorType;
        ErrorMessage = errorMessage;
    }
}