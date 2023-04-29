using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using DungeonDiscordBot.Model;

using Microsoft.Extensions.DependencyInjection;

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
    
    public override async Task Init()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddAudioBypass();
        
        _api = new VkApi(services);   
        await _api.AuthorizeAsync(new ApiAuthParams {
            Login = ApiCredentials.VK_LOGIN,
            Password = ApiCredentials.VK_PASSWORD,
        });
    }

    public override async Task<IEnumerable<AudioQueueRecord>> GetAudiosFromLink(Uri link)
    {
        List<AudioQueueRecord> records = new List<AudioQueueRecord>();
        
        string url = link.AbsoluteUri;
        if (url.Contains("playlist")) {
            Regex regex = new Regex(@".+playlist/(\d+)_(\d+)(_(.+))?");
            Match match = regex.Match(url);

            int userId = Convert.ToInt32(match.Groups[1].Value);
            int playlistId = Convert.ToInt32(match.Groups[2].Value);

            string accessToken = "";
            if (match.Groups.Count == 5) {
                accessToken = match.Groups[4].Value;
            }
            
            VkCollection<Audio> audios = await _api.Audio.GetAsync(new AudioGetParams {
                PlaylistId = playlistId,
                OwnerId = userId,
                AccessKey = accessToken
            });

            OnAudiosProcessingStarted(audios.Count);
            int addedCount = 0;
            for (int i = 0; i < audios.Count; i++) {
                Audio audio = audios[i];
                if (audio.Url is null) {
                    continue;
                }
                
                records.Add(new AudioQueueRecord(audio.Artist, audio.Title, audio.Url, audio.Album?.Thumb.Photo135));
                addedCount++;
            }
            
            OnAudiosProcessed(addedCount);
        } else if (url.Contains("audio")) {
            Regex regex = new Regex(@".+audio(\d+)_(\d+)_(.+)");
            Match match = regex.Match(url);

            long userId = Convert.ToInt64(match.Groups[1].Value);
            long audioId = Convert.ToInt64(match.Groups[2].Value);
            string accessToken = match.Groups[3].Value;
            
            VkCollection<Audio> audios = await _api.Audio.GetAsync(new AudioGetParams {
                AudioIds = new [] {
                    audioId
                },
                OwnerId = userId,
                AccessKey = accessToken
            });

            if (audios.Count == 0) {
                throw new ArgumentException("Audio not found by this link", nameof(link));
            }
            
            OnAudiosProcessingStarted(1);
            
            Audio audio = audios.First();
            if (audio.Id is null) {
                OnAudiosProcessed(0);
                return records;
            }

            records.Add(new AudioQueueRecord(audio.Artist, audio.Title, audio.Url, audio.Album?.Thumb.Photo135));
            OnAudiosProcessed(1);
        }

        return records;
    }
}