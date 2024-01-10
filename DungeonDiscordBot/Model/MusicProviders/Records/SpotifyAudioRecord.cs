using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos.Streams;

namespace DungeonDiscordBot.Model.MusicProviders.Records;

public class SpotifyAudioRecord : AudioQueueRecord
{
    private string? _youtubeVideoId;
    private readonly YoutubeClient _youtubeApi;
    
    public SpotifyAudioRecord(
        YoutubeClient youtubeApi, 
        MusicCollectionMetadata metadata, string author, string title, 
        Func<Task<string?>> audioThumbnailUriFactory, 
        TimeSpan duration, string? publicUrl) 
        : base(MusicProvider.Spotify, metadata, author, title, audioThumbnailUriFactory, duration, publicUrl)
    {
        _youtubeApi = youtubeApi;
    }

    public override async Task UpdateAudioUrlAsync()
    {
        if (_youtubeVideoId is null) {
            VideoSearchResult? result = await _youtubeApi.Search.GetVideosAsync($"{Author} - {Title}")
                .FirstOrDefaultAsync();
            
            if (result is null) {
                throw new ArgumentException("Corresponding YouTube video ID was not found");
            }

            _youtubeVideoId = result.Id;
        }
        
        StreamManifest manifest = await _youtubeApi.Videos.Streams
            .GetManifestAsync($"https://youtube.com/watch?v={_youtubeVideoId}");
        AudioUrl = manifest.GetAudioOnlyStreams().GetWithHighestBitrate().Url;
    }
}