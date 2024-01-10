using DungeonDiscordBot.InternalAPIs.SpotifyDown;

using JetBrains.Annotations;

using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace DungeonDiscordBot.Model.MusicProviders.Records;

public class SpotifyAudioRecord : AudioQueueRecord
{
    private string? _youtubeVideoId;
    private readonly string _trackId;
    private readonly YoutubeClient _youtubeApi;
    private readonly ISpotifyDownApi _spotifyDownApi;
    
    public SpotifyAudioRecord(
        YoutubeClient youtubeApi, ISpotifyDownApi spotifyDownApi, string trackId, 
        MusicCollectionMetadata metadata, string author, string title, 
        Func<Task<string?>> audioThumbnailUriFactory, 
        TimeSpan duration, string? publicUrl) 
        : base(MusicProvider.Spotify, metadata, author, title, audioThumbnailUriFactory, duration, publicUrl)
    {
        _trackId = trackId;
        _youtubeApi = youtubeApi;
        _spotifyDownApi = spotifyDownApi;
    }

    public override async Task UpdateAudioUrlAsync()
    {
        if (_youtubeVideoId is null) {
            _youtubeVideoId = await _spotifyDownApi.GetYoutubeIdAsync(_trackId);
            if (_youtubeVideoId is null) {
                throw new ArgumentException("Corresponding YouTube video ID was not found", nameof(_trackId));
            }
        }
        
        StreamManifest manifest = await _youtubeApi.Videos.Streams
            .GetManifestAsync($"https://youtube.com/watch?v={_youtubeVideoId}");
        AudioUrl = manifest.GetAudioOnlyStreams().GetWithHighestBitrate().Url;
    }
}