using JetBrains.Annotations;

using VkNet.AudioApi;
using VkNet.AudioApi.Model;

namespace DungeonDiscordBot.Model.MusicProviders.Records;

public class VkAudioRecord : AudioQueueRecord
{
    private readonly Audio _vkAudio;
    private readonly IVkAudioApi _api;
    
    public VkAudioRecord(
        IVkAudioApi api, Audio vkAudio, MusicCollectionMetadata metadata, string author, string title,
        string? audioThumbnailUri, TimeSpan duration, string? publicUrl) 
        : base(MusicProvider.Spotify, metadata, author, title, async () => audioThumbnailUri, duration, publicUrl)
    {
        _api = api;
        _vkAudio = vkAudio;
        AudioUrl = _vkAudio.Url;
    }

    public override async Task UpdateAudioUrlAsync()
    {
        List<Audio> audios = await _api.GetAudiosByIdAsync(new[] { $"{_vkAudio.Id}_{_vkAudio.OwnerId}" });
        AudioUrl = audios.First().Url;
    }
}