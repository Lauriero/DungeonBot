using System.Text.RegularExpressions;

using DungeonDiscordBot.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

using VkNet;
using VkNet.Abstractions;
using VkNet.AudioBypassService.Extensions;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
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
        _logger.LogInformation("VK Music provider initialization started");

        ServiceCollection services = new ServiceCollection();
        services.AddAudioBypass();
        
        _api = new VkApi(services);   
        await _api.AuthorizeAsync(new ApiAuthParams {
            Login = _settings.VKLogin,
            Password = _settings.VKPassword,
        });
        
        _logger.LogInformation("VK Music provider initialized");
    }

    public override async Task<IEnumerable<AudioQueueRecord>> GetAudiosFromLinkAsync(Uri link)
    {
        List<AudioQueueRecord> records = new List<AudioQueueRecord>();
        
        string url = link.AbsoluteUri;
        Regex songRegex = new Regex(@".+audio(\d+)_(\d+)_(.+)");
        Regex playlistRegex = new Regex(@".+playlist/([\d-]+)_(\d+)(_(.+))?");
        Regex sovaPlaylistRegex = new Regex(@".+audio_playlist([\d-]+)_(\d+)/(_(.+))?");

        Match songMatch = songRegex.Match(url);
        Match playlistMatch = playlistRegex.Match(url);
        Match sovaPlaylistMatch = sovaPlaylistRegex.Match(url);

        long userId;
        long? playlistId;
        string accessToken;
        IEnumerable<Audio> audios;
        if (songMatch.Success) {
            userId = Convert.ToInt64(songMatch.Groups[1].Value);
            long audioId = Convert.ToInt64(songMatch.Groups[2].Value);
            accessToken = songMatch.Groups[3].Value;
            
            audios = await GetAudio(userId, audioId, accessToken);
        } else if (playlistMatch.Success) {
            userId = Convert.ToInt64(playlistMatch.Groups[1].Value);
            playlistId = Convert.ToInt64(playlistMatch.Groups[2].Value);
            accessToken = "";
            if (playlistMatch.Groups.Count == 5) {
                accessToken = playlistMatch.Groups[4].Value;
            }

            audios = await GetAudios(userId, playlistId.Value, accessToken);
        } else if (sovaPlaylistMatch.Success) {
            userId = Convert.ToInt64(sovaPlaylistMatch.Groups[1].Value);
            playlistId = Convert.ToInt64(sovaPlaylistMatch.Groups[2].Value);
            accessToken = "";
            if (playlistMatch.Groups.Count == 5) {
                accessToken = sovaPlaylistMatch.Groups[4].Value;
            }

            audios = await GetAudios(userId, playlistId.Value, accessToken);
        } else {
            throw new ArgumentException("Link is not supported", nameof(url));
        }

        OnAudiosProcessingStarted(audios.Count());
        int addedCount = 0;
        for (int i = 0; i < audios.Count(); i++) {
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
        return records;
    }

    public override async Task<AudioQueueRecord?> GetAudioFromSearchQueryAsync(string query)
    {
        VkCollection<Audio> audios = await _api.Audio.SearchAsync(new AudioSearchParams {
            Query = query,
            Count = 1,
            Autocomplete = true
        });

        if (audios.Count == 0) {
            return null;
        }

        Audio audio = audios.First();

        return new AudioQueueRecord(audio.Artist, audio.Title,
            () => Task.FromResult(audio.Url.AbsoluteUri),
            () => Task.FromResult(audio.Album?.Thumb.Photo135),
            TimeSpan.FromSeconds(audio.Duration));
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