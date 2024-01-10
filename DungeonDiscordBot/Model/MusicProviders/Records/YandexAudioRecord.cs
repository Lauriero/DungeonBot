using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using Yandex.Music.Api;
using Yandex.Music.Api.Common;
using Yandex.Music.Api.Models.Track;

namespace DungeonDiscordBot.Model.MusicProviders.Records;

public class YandexAudioRecord : AudioQueueRecord
{
    private readonly YTrack _track;
    private readonly YandexMusicApi _api;
    private readonly AuthStorage _apiAuth;
    
    public YandexAudioRecord(YandexMusicApi api, AuthStorage apiAuth, YTrack track, 
        MusicCollectionMetadata metadata, string author, string title, 
        Func<Task<string?>> audioThumbnailUriFactory, 
        TimeSpan duration, string? publicUrl) 
        : base(MusicProvider.Yandex, metadata, author, title, audioThumbnailUriFactory, duration, publicUrl)
    {
        _api = api;
        _track = track;
        _apiAuth = apiAuth;
    }
    
    public override async Task UpdateAudioUrlAsync()
    {
        AudioUrl = await _api.Track.GetFileLinkAsync(_apiAuth, _track);
    }
}