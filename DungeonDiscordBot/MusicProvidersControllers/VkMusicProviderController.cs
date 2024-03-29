﻿using System.Text.RegularExpressions;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.Model.MusicProviders.Records;
using DungeonDiscordBot.Model.MusicProviders.Search;
using DungeonDiscordBot.Settings;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

using VkNet.Abstractions;
using VkNet.AudioApi;
using VkNet.AudioApi.Model;
using VkNet.Enums.Filters;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VkNet.Utils;

using Group = VkNet.AudioApi.Model.Group;

namespace DungeonDiscordBot.MusicProvidersControllers;

public class VkMusicProviderController : BaseMusicProviderController
{
    public override string DisplayName => "VK";
    public override string LinksDomainName => "vk.com";
    public override string LogoEmojiId => "<:logo_vk:1189750001262403604>";
    public override string LogoUri => "http://larc.tech/content/dungeon-bot/logo-vk.png";
    public override string SupportedLinks =>
        "Use https://vk.com/audio{trackData} to retrieve a single track\n" +
        "Use https://vk.com/music/album/{albumData} to retrieve tracks from the album\n" +
        "Use https://vk.com/music/playlist/{playlistData} to retrieve tracks from the playlist\n" +
        "Use https://vk.com/audio_playlist{playlistData} to retrieve tracks from the sova playlist";

    private readonly IVkApi _vkApi;
    private readonly IVkAudioApi _audioApi;
    private readonly ILogger<VkMusicProviderController> _logger;
    private readonly AppSettings _settings;

    public VkMusicProviderController(IOptions<AppSettings> settings, ILogger<VkMusicProviderController> logger, 
        IVkAudioApi audioApi, IVkApi vkApi)
    {
        _audioApi = audioApi;
        _vkApi = vkApi;
        _logger = logger;
        _settings = settings.Value;
    }
    
    public override async Task InitializeAsync()
    {
        await _audioApi.AuthAsync(_settings.VKLogin, _settings.VKPassword, () => {
            Console.Write("Insert code: ");
            return Console.ReadLine()!;
        });
    }

    public override async Task<MusicCollectionResponse> GetAudiosFromLinkAsync(Uri link, int count)
    {
        string url = link.AbsoluteUri;
        Regex songRegex = new Regex(@".+audio(-?\d+)_(\d+)(_(.+))?");
        Regex albumRegex = new Regex(@".+album/([\d-]+)_(\d+)(_(.+))?");
        Regex playlistRegex = new Regex(@".+playlist/([\d-]+)_(\d+)(_(.+))?");
        Regex sovaPlaylistRegex = new Regex(@".+audio_playlist([\d-]+)_(\d+)/(_(.+))?");

        Match songMatch = songRegex.Match(url);
        Match albumMatch = albumRegex.Match(url);
        Match playlistMatch = playlistRegex.Match(url);
        Match sovaPlaylistMatch = sovaPlaylistRegex.Match(url);

        long userId;
        long? playlistId;
        string accessToken;
        List<Audio> audios = new List<Audio>();
        MusicCollectionMetadata metadata = new MusicCollectionMetadata { PublicUrl = link.AbsoluteUri };
        if (songMatch.Success) {
            userId = Convert.ToInt64(songMatch.Groups[1].Value);
            long audioId = Convert.ToInt64(songMatch.Groups[2].Value);

            try {
                audios.AddRange(await _audioApi.GetAudiosByIdAsync(
                    new[] { $"{userId}_{audioId}" }));
            } catch (AudioAccessDeniedException) {
                return MusicCollectionResponse.FromError(MusicProvider.VK, MusicResponseErrorType.PermissionDenied, 
                    "Access to audio is denied");
            }
            
            if (!audios.Any()) {
                return MusicCollectionResponse.FromError(MusicProvider.VK, MusicResponseErrorType.NoAudioFound, 
                    $"There's no audio found on {url}");
            }

            Audio firstAudio = audios.First();
            if (string.IsNullOrEmpty(firstAudio.Url)) {
                return MusicCollectionResponse.FromError(MusicProvider.VK, MusicResponseErrorType.PermissionDenied, 
                    "Access to audio is denied");
            }

            metadata.Name = $"{firstAudio.Artist} - {firstAudio.Title}";
            metadata.Type = MusicCollectionType.Track;
        } else if (playlistMatch.Success) {
            await onPlaylistMatch(playlistMatch);
        } else if (sovaPlaylistMatch.Success) {
            await onPlaylistMatch(sovaPlaylistMatch);
        } else if (albumMatch.Success) {
            userId = Convert.ToInt64(albumMatch.Groups[1].Value);
            playlistId = Convert.ToInt64(albumMatch.Groups[2].Value);
            accessToken = "";
            if (albumMatch.Groups.Count == 5) {
                accessToken = albumMatch.Groups[4].Value;
            }
            
            audios.AddRange(await _audioApi.AudioGetAsync(
                playlistId.Value,
                userId, 
                accessToken));
            
            if (!audios.Any()) {
                return MusicCollectionResponse.FromError(MusicProvider.VK, MusicResponseErrorType.NoAudioFound, 
                    "There's nothing in the requested album");
            }

            Audio firstAudio = audios.First();
            metadata.Name = $"{firstAudio.Artist} - {firstAudio.Album!.Title}";
            metadata.Type = MusicCollectionType.Album;
        } else {
            return MusicCollectionResponse.FromError(MusicProvider.VK, MusicResponseErrorType.LinkNotSupported, 
                $"Current provider can't handle urls like {url}");
        }

        async Task onPlaylistMatch(Match match)
        {
            userId = Convert.ToInt64(match.Groups[1].Value);
            playlistId = Convert.ToInt64(match.Groups[2].Value);
            accessToken = "";
            if (match.Groups.Count == 5) {
                accessToken = match.Groups[4].Value;
            }
            
            Playlist playlist = await _audioApi.GetPlaylistAsync(playlistId.Value, accessToken, userId);

            if (playlist.OwnerId > 0) {
                User playlistOwner = (await _audioApi.GetUserAsync(playlist.OwnerId))!;
                metadata.Name = $"{playlistOwner.FirstName} {playlistOwner.LastName} - {playlist.Title}";
            } else {
                long groupId = -1 * userId;
                Group? group = await _audioApi.GetGroupByIdAsync(
                    groupIds: new []{ groupId.ToString() },
                    fields: GroupsFields.Description
                );

                if (group is null) {
                    return;
                }
                
                metadata.Name = $"{group.Name} - {playlist.Title}";
            }

            metadata.Type = MusicCollectionType.Playlist;
            audios.AddRange(await _audioApi.AudioGetAsync(
                playlistId.Value, 
                userId, 
                accessToken));
        }

        int addedAudiosCount = 0;
        List<AudioQueueRecord> records = new List<AudioQueueRecord>();
        for (int i = 0; i < (count > -1 ? count : audios.Count); i++) {
            Audio audio = audios.ElementAt(i);
            if (string.IsNullOrEmpty(audio.Url)) {
                continue;
            }

            addedAudiosCount++;
            records.Add(new VkAudioRecord(
                _audioApi, audio, 
                metadata: metadata,
                author:             audio.Artist, 
                title:              audio.Title,
                audioThumbnailUri:  audio.Album?.Thumb?.Photo135,
                duration:           TimeSpan.FromSeconds(audio.Duration),
                publicUrl:          $"https://vk.com/audio{audio.OwnerId}_{audio.Id}_{audio.AccessKey}"));
        }

        if (addedAudiosCount == 0) {
            return MusicCollectionResponse.FromError(MusicProvider.VK, MusicResponseErrorType.NoAudioFound, 
                    "There's nothing in the requested album");
        }
        
        return MusicCollectionResponse.FromSuccess(MusicProvider.VK, metadata, records);
    }

    public override async Task<MusicSearchResult> SearchAsync(string query, MusicCollectionType targetCollectionType, int? count = null)
    {
        VkCollection<VkNet.Model.Attachments.Audio> audios = await _vkApi.Audio.SearchAsync(new AudioSearchParams {
            Query = query,
            Autocomplete = true,
            Count = count ?? MaxSearchResultsCount
        });
        return new MusicSearchResult(
            provider: MusicProvider.VK,
            entities: audios
                .Select(a => new SearchResultEntity(
                    name: $"{a.Artist} - {a.Title}",
                    link: $"https://vk.com/audio{a.OwnerId}_{a.Id}_{a.AccessKey}")));
    }
}