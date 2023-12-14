using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

using VkNet;
using VkNet.Abstractions;
using VkNet.AudioBypassService.Extensions;
using VkNet.Model;
using VkNet.Utils;

namespace DungeonDiscordBot.MusicProvidersControllers;

public class VkMusicProviderController : BaseMusicProviderController
{
    public override string LinksDomainName => "vk.com";

    private IVkApi _api = null!;
    private readonly ILogger<VkMusicProviderController> _logger;
    private readonly AppSettings _settings;

    public VkMusicProviderController(IOptions<AppSettings> settings, ILogger<VkMusicProviderController> logger)
    {
        _logger = logger;
        _settings = settings.Value;
    }
    
    public override async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing VKMusic provider...");

        ServiceCollection services = new ServiceCollection();
        services.AddAudioBypass();
        
        _api = new VkApi(services);   
        await _api.AuthorizeAsync(new ApiAuthParams {
            Login = _settings.VKLogin,
            Password = _settings.VKPassword,
        });
        
        _logger.LogInformation("VKMusic provider initialized");
    }

    public override async Task<MusicCollection> GetAudiosFromLinkAsync(Uri link, int count)
    {
        string url = link.AbsoluteUri;
        Regex songRegex = new Regex(@".+audio(-?\d+)_(\d+)_(.+)");
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
        if (songMatch.Success) {
            userId = Convert.ToInt64(songMatch.Groups[1].Value);
            long audioId = Convert.ToInt64(songMatch.Groups[2].Value);
            accessToken = songMatch.Groups[3].Value;;

            audios.AddRange(await GetAudio(userId, audioId, accessToken));
            if (!audios.Any()) {
                return new MusicCollection(MusicProvider.VK, "Not found", Array.Empty<AudioQueueRecord>());
            }

            Audio firstAudio = audios.First();
            return new MusicCollection(MusicProvider.VK, 
                name: $"{firstAudio.Artist} - {firstAudio.Title}",
                audios: new [] {
                    new AudioQueueRecord(firstAudio.Artist, firstAudio.Title,
                        () => Task.FromResult(firstAudio.Url.AbsoluteUri),
                        () => Task.FromResult(firstAudio.Album?.Thumb.Photo135),
                        TimeSpan.FromSeconds(firstAudio.Duration))
                }
            );
        }

        string collectionName = "Not found";
        if (playlistMatch.Success) {
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
            
            audios.AddRange(await GetAudios(userId, playlistId.Value, accessToken));
            if (!audios.Any()) {
                return new MusicCollection(MusicProvider.Yandex, "Not found", Array.Empty<AudioQueueRecord>());
            }

            Audio firstAudio = audios.First();
            collectionName = $"{firstAudio.Artist} - {firstAudio.Album.Title}";
        } else {
            throw new ArgumentException("Link is not supported", nameof(url));
        }

        async Task onPlaylistMatch(Match match)
        {
            userId = Convert.ToInt64(match.Groups[1].Value);
            playlistId = Convert.ToInt64(match.Groups[2].Value);
            accessToken = "";
            if (match.Groups.Count == 5) {
                accessToken = match.Groups[4].Value;
            }
            
            AudioPlaylist playlist = await _api.Audio.GetPlaylistByIdAsync(userId, playlistId.Value);
            User playlistOwner = (await _api.Users.GetAsync(new[] {playlist.OwnerId!.Value})).First();
            collectionName = $"{playlistOwner.FirstName} {playlistOwner.LastName} - {playlist.Title}";
            
            audios.AddRange(await GetAudios(userId, playlistId.Value, accessToken));
        }

        int toAddCount = audios.Count;
        if (count > -1) {
            toAddCount = count;
        }
        
        int addedCount = 0;
        OnAudiosProcessingStarted(audios.Count);
        
        List<AudioQueueRecord> records = new List<AudioQueueRecord>();
        for (int i = 0; i < toAddCount; i++) {
            Audio audio = audios.ElementAt(i);
            if (audio.Url is null) {
                continue;
            }
            
            records.Add(new AudioQueueRecord(audio.Artist, audio.Title, 
                () => Task.FromResult(audio.Url.AbsoluteUri), 
                () => Task.FromResult(audio.Album?.Thumb.Photo135),
                TimeSpan.FromSeconds(audio.Duration)));
            addedCount++;
        }
        
        OnAudiosProcessed(addedCount);
        return new MusicCollection(MusicProvider.VK, collectionName, records);
    }

    public override async Task<MusicCollection> GetAudioFromSearchQueryAsync(string query)
    {
        VkCollection<Audio> audios = await _api.Audio.SearchAsync(new AudioSearchParams {
            Query = query,
            Count = 1,
            Autocomplete = true
        });

        if (audios.Count == 0) {
            return new MusicCollection(MusicProvider.VK, "Not found", Array.Empty<AudioQueueRecord>());
        }

        Audio audio = audios.First();
        return new MusicCollection(MusicProvider.VK, 
            name: $"{audio.Artist} - {audio.Title}",
            audios: new [] {
                new AudioQueueRecord(audio.Artist, audio.Title,
                    () => Task.FromResult(audio.Url.AbsoluteUri),
                    () => Task.FromResult(audio.Album?.Thumb.Photo135),
                    TimeSpan.FromSeconds(audio.Duration))
            }
        );
    }

    public async Task<IEnumerable<Audio>> GetAudios(long userId, long playlistId, string accessKey)
    {
        VkCollection<Audio> audios = await _api.Audio.GetAsync(new AudioGetParams {
            PlaylistId = playlistId,
            OwnerId = userId,
            AccessKey = accessKey
        });

        return audios;
    }
    
    public async Task<IEnumerable<Audio>> GetAudio(long userId, long audioId, string accessKey)
    {
        VkCollection<Audio> audios = await _api.Audio.GetAsync(new AudioGetParams {
            AudioIds = new [] {
                audioId
            },
            OwnerId = userId,
            AccessKey = accessKey
        });

        return audios;
    }
}