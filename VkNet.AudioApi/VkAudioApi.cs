using System.Diagnostics;
using System.Net.Http.Headers;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using VkNet.Abstractions;
using VkNet.Abstractions.Core;
using VkNet.AudioApi.AudioBypassService.Abstractions;
using VkNet.AudioApi.AudioBypassService.Models.Auth;
using VkNet.AudioApi.DIExtensions;
using VkNet.AudioApi.Model;
using VkNet.AudioApi.Model.General;
using VkNet.Enums.Filters;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using VkNet.Utils;

using Audio = VkNet.AudioApi.Model.Audio;
using Group = VkNet.Model.Group;
using Lyrics = VkNet.AudioApi.Model.Lyrics;

namespace VkNet.AudioApi
{
    public class VkAudioApi : IVkAudioApi
    {
        public readonly IVkApiCategories vkApi;
        private readonly IVkApiInvoke apiInvoke;
        private readonly ILogger _logger;
        private readonly string vkApiVersion = "5.220";

        public bool IsAuth = false;
        private readonly IVkTokenStore tokenStore;
        private readonly IVkApiAuthAsync auth;
        private readonly IVkApi _api;
        private readonly IDeviceIdStore _deviceIdStore;
        private readonly ITokenRefreshHandler _tokenRefreshHandler;

        public VkAudioApi(ILogger<VkAudioApi> logger, IVkApiCategories vkApi, IVkApiInvoke apiInvoke, IVkApiVersionManager versionManager,
                         IVkTokenStore tokenStore, IVkApiAuthAsync auth, IVkApi api, IDeviceIdStore deviceIdStore, ITokenRefreshHandler tokenRefreshHandler)
        {
            this._logger = logger;
            this.vkApi = vkApi;
            this.apiInvoke = apiInvoke;
            this.tokenStore = tokenStore;
            this.auth = auth;
            _api = api;
            _deviceIdStore = deviceIdStore;
            _tokenRefreshHandler = tokenRefreshHandler;

            string[] ver = vkApiVersion.Split('.');
            versionManager.SetVersion(int.Parse(ver[0]), int.Parse(ver[1]));
        }
        

        public async Task<string> AuthAsync(string login, string password, Func<string> twoFactorAuth)
        {
            try
            {
                _logger.LogInformation("Invoke auth with login and password");
                
                await auth.AuthorizeAsync(new AndroidApiAuthParams());
                await auth.AuthorizeAsync(new AndroidApiAuthParams(login, password));
                
                var user = await vkApi.Users.GetAsync(new List<long>());
                _api.UserId = user[0].Id;
                IsAuth = true;
                
                _logger.LogInformation($"User '{_api.UserId}' successful sign in");

                return tokenStore.Token;
            }catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
           
        }

        public async Task SetTokenAsync(string token)
        {
            try
            {
                _logger.LogInformation("Set user token");

                await auth.AuthorizeAsync(new ApiAuthParams()
                {
                    AccessToken = token
                });

                try
                {
                    var user = await vkApi.Users.GetAsync(new List<long>());
                    _api.UserId = user[0].Id;
                    _logger.LogInformation($"User '{user[0].Id}' successful sign in");
                }
                catch (VkApiMethodInvokeException e) when (e.ErrorCode == 1117) // token has expired
                {
                    if (await _tokenRefreshHandler.RefreshTokenAsync(token) is null)
                        throw;
                }

                IsAuth = true;
            }catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<VkNet.AudioApi.Model.Group?> GetGroupByIdAsync(IEnumerable<string> groupIds, GroupsFields fields)
        {
            try
            {
                _logger.LogInformation("Invoke 'catalog.getAudio' ");
                VkParameters parameters = new VkParameters
                {
                    {"extended", "1"},
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {
                      "group_ids",
                      groupIds
                    },
                    {
                      nameof (fields),
                      fields
                    }
                };
                
                string? json = await apiInvoke.InvokeAsync("groups.getById", parameters);
                _logger.LogDebug("RESULT OF 'groups.getById'" + json);

                ResponseData? model = JsonConvert.DeserializeObject<ResponseData>(json)!;

                _logger.LogInformation("Successful invoke 'groups.getById' ");

                return model.Groups.FirstOrDefault();
            } catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<List<Audio>> SearchAudioAsync(string query, string? context = null)
        {
            ResponseData audioCatalog = await GetAudioSearchCatalogAsync(query);
            ResponseData section = await GetSectionAsync(audioCatalog.Catalog.Sections[0].Id);
            return section.Audios ?? new List<Audio>();
        }
        
        public async Task<ResponseData> GetAudioCatalogAsync(string? url = null)
        {
            try
            {
                _logger.LogInformation("Invoke 'catalog.getAudio' ");

                VkParameters parameters = new VkParameters
                {
                    
                    {"extended", "1"},
                    {"need_blocks", "1"},
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()}
                };

                if(url != null)
                {
                    parameters.Add("url", url);
                    parameters.Add("ref", url);
                }

                string? json = await apiInvoke.InvokeAsync("catalog.getAudio", parameters);
                _logger.LogDebug("RESULT OF 'catalog.getAudio'" + json);

                ResponseData? model = JsonConvert.DeserializeObject<ResponseData>(json);

                _logger.LogInformation("Successful invoke 'catalog.getAudio' ");

                return model!;
            }catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
        
        public async Task<ResponseData> GetSectionAsync(string sectionId, string? startFrom = null)
        {
            try
            {
                _logger.LogInformation($"Invoke 'catalog.getSection' with sectionId = '{sectionId}' ");

                var parameters = new VkParameters
                {
                    
                    {"extended", "1"},
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    {"section_id", sectionId },
                    {"need_blocks", 0 },
                };

                if (startFrom != null) parameters.Add("start_from", startFrom);


                var json = await apiInvoke.InvokeAsync("catalog.getSection", parameters);
                _logger.LogDebug("RESULT OF 'catalog.getSection'" + json);


                ResponseData model = JsonConvert.DeserializeObject<ResponseData>(json)!;

                _logger.LogInformation("Successful invoke 'catalog.getSection' ");
                
                return model;
            } catch (System.Exception ex) {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
        
        public async Task<ResponseData> GetBlockItemsAsync(string blockId)
        {
            try
            {
                _logger.LogInformation($"Invoke 'catalog.getBlockItems' with blockId = '{blockId}' ");

                VkParameters parameters = new VkParameters
                {
                    
                    {"extended", "1"},
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"block_id", blockId },
                };


                string? json = await apiInvoke.InvokeAsync("catalog.getBlockItems", parameters);
                _logger.LogDebug("RESULT OF 'catalog.getBlockItems'" + json);


                ResponseData? model = JsonConvert.DeserializeObject<ResponseData>(json);

                _logger.LogInformation("Successful invoke 'catalog.getBlockItems' ");

                return model!;
            }catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<ResponseData> GetAudioSearchCatalogAsync(string? query = null, string? context= null)
        {
            try
            {
                _logger.LogInformation($"Invoke 'catalog.getAudioSearch' with query = '{query}' ");

                VkParameters parameters = new VkParameters
                {
                    {"extended", "1"},
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                };

                if (query != null)
                {
                    parameters.Add("query", query);
                }
                else parameters.Add("need_blocks", "1");
                if (context != null) parameters.Add("context", context);


                string? json = await apiInvoke.InvokeAsync("catalog.getAudioSearch", parameters);
                _logger.LogDebug("RESULT OF 'catalog.getAudioSearch'" + json);


                ResponseData? model = JsonConvert.DeserializeObject<ResponseData>(json);
                _logger.LogInformation("Successful invoke 'catalog.getAudioSearch' ");

                return model!;
            }catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<ResponseData> GetAudioArtistAsync(string artistId)
        {
            try
            {
                _logger.LogInformation($"Invoke 'catalog.getAudioArtist' with artistId = '{artistId}' ");

                VkParameters parameters = new VkParameters
                {
                    
                    {"extended", "1"},
                    
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    {"artist_id", artistId}

                };


                string? json = await apiInvoke.InvokeAsync("catalog.getAudioArtist", parameters);
                _logger.LogDebug("RESULT OF 'catalog.getAudioArtist'" + json);


                ResponseData? model = JsonConvert.DeserializeObject<ResponseData>(json);

                _logger.LogInformation("Successful invoke 'catalog.getAudioArtist' ");


                return model!;
            }catch (System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<ResponseData> GetAudioCuratorAsync(string curatorId, string url)
        {
            try
            {
                _logger.LogInformation($"Invoke 'catalog.getAudioCurator' with curatorId = '{curatorId}' ");

                VkParameters parameters = new VkParameters
                {
                    
                    {"extended", "1"},
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"curator_id", curatorId},
                    {"url", url}
                };


                string? json = await apiInvoke.InvokeAsync("catalog.getAudioCurator", parameters);
                _logger.LogDebug("RESULT OF 'catalog.getAudioCurator'" + json);


                ResponseData? model = JsonConvert.DeserializeObject<ResponseData>(json);

                _logger.LogInformation("Successful invoke 'catalog.getAudioCurator' ");


                return model!;
            }catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<Playlist> GetPlaylistAsync(long albumId, string accessKey, long ownerId, int offset = 0, int count = 100, int needOwner = 1)
        {
            try
            {
                _logger.LogInformation($"Invoke 'execute.getPlaylist' with albumId = '{albumId}' ");

                VkParameters parameters = new VkParameters
                {
                    {"v", vkApiVersion },

                    {"lang", "ru"},
                    {"audio_count", count },
                    {"need_playlist", 1 },
                    {"owner_id", ownerId},
                    {"access_key", accessKey},
                    {"func_v", 10 },
                    {"id", albumId},
                    
                    {"audio_offset", offset },
                    {"count", count},
                    {"need_owner", needOwner }
                };

                string? json  = await apiInvoke.InvokeAsync("execute.getPlaylist", parameters);
                _logger.LogDebug("RESULT OF 'execute.getPlaylist'" + json);


                ResponseData model = JsonConvert.DeserializeObject<ResponseData>(json)!;

                _logger.LogInformation("Successful invoke 'execute.getPlaylist' ");
                
                return model.Playlist;
            }catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }

        }

        public async Task AudioAddAsync(long audioId, long ownerId)
        {
            try
            {
                _logger.LogInformation($"Invoke 'audio.add' with audioId = '{audioId}' and ownerId = '{ownerId}' ");

                VkParameters parameters = new VkParameters
                {
                    
                    
                    {"audio_id", audioId},
                    {"owner_id", ownerId}
                };


                string? json = await apiInvoke.InvokeAsync("audio.add", parameters);
                _logger.LogDebug("RESULT OF 'audio.add'" + json);


                _logger.LogInformation("Successful invoke 'audio.add' ");
            }catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task AudioDeleteAsync(long audioId, long ownerId)
        {
            try
            {
                _logger.LogInformation($"audio.delete' with audioId = '{audioId}' and ownerId = '{ownerId}' ");

                VkParameters parameters = new VkParameters
                {
                    
                    
                    {"audio_id", audioId},
                    {"owner_id", ownerId}
                };


                string? json = await apiInvoke.InvokeAsync("audio.delete", parameters);

                _logger.LogDebug("RESULT OF 'audio.delete'" + json);


                _logger.LogInformation("Successful invoke 'audio.delete' ");
            }catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                var users = await vkApi.Users.GetAsync(new List<long>(), ProfileFields.Photo200);
                User? currentUser = users?.FirstOrDefault();
                return currentUser;
            }catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
           
        }

        public async Task<User?> GetUserAsync(long userId)
        {
            try
            {
                var users = await vkApi.Users.GetAsync(new List<long> { userId }, ProfileFields.Photo200);
                User? currentUser = users?.FirstOrDefault();
                return currentUser;
            }catch (System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
           
        }

        public async Task<Owner?> OwnerAsync(long ownerId)
        {
            try {
                if (ownerId > 0)
                {
                    var users = await vkApi.Users.GetAsync(new List<long>() { ownerId });
                    User? user = users?.FirstOrDefault();
                    if (user != null)
                    {
                        Owner owner = new Owner()
                        {
                            Id = user.Id,
                            Name = user.LastName + " " + user.FirstName
                        };
                        return owner;
                    }

                    return null;
                }

                ownerId *= (-1);
                var groups = await vkApi.Groups.GetByIdAsync(new List<string>() { ownerId.ToString() }, "",
                    GroupsFields.Description);
                Group? group = groups?.FirstOrDefault();
                if (@group != null)
                {
                    Owner owner = new Owner()
                    {
                        Id = ownerId,
                        Name = @group.Name,
                    };
                    return owner;
                }

                return null;
            } catch (System.Exception ex) {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task AddPlaylistAsync(long playlistId, long ownerId, string accessKey)
        {
            try
            {
                _logger.LogInformation($"audio.followPlaylist' with playlistId = '{playlistId}' and ownerId = '{ownerId}' and accessKey = {accessKey} ");

                VkParameters parameters = new VkParameters
                {
                    
                    
                    {"playlist_id", playlistId},
                    {"owner_id", ownerId},
                    
                };

                parameters.Add("access_key", accessKey);

                string? json = await apiInvoke.InvokeAsync("audio.followPlaylist", parameters);

                _logger.LogDebug("RESULT OF 'audio.followPlaylist'" + json);


                _logger.LogInformation("Successful invoke 'audio.followPlaylist' ");
            }catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
            

        }

        public async Task DeletePlaylistAsync(long playlistId, long ownerId)
        {
            try
            {
                bool result = await vkApi.Audio.DeletePlaylistAsync(ownerId, playlistId);

            }catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
        
        public async Task<List<Audio>> GetAudiosByIdAsync(IEnumerable<string> audios)
        {
            try {
                _logger.LogInformation($"invoke 'audio.getById' with " +
                                      $"audios = '{string.Join(',', audios)}'");
                
                VkParameters parameters = new VkParameters {
                    {"audios", string.Join(',', audios)}
                };
                
                string? json = await apiInvoke.InvokeAsync("audio.getById", parameters);
                _logger.LogDebug("RESULT OF 'audio.getById'" + json);

                List<Audio> model = JsonConvert.DeserializeObject<List<Audio>>(json)!;
                _logger.LogInformation("Successful invoke 'audio.getById' ");
                return model;
            } catch(System.Exception ex) {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<List<Audio>> AudioGetAsync(long? playlistId, long? ownerId, string? assessKey, long offset = 0, long count = 100)
        {
            try
            {
                _logger.LogInformation($"invoke 'audio.get' with " +
                                      $"playlistId = '{playlistId}' and ownerId = '{ownerId}' and accessKey = {assessKey} ");
                
                VkParameters parameters = new VkParameters
                {
                    {"offset", offset },
                    {"count", count }
                };

                if (playlistId != null) {
                    parameters.Add("playlist_id", playlistId);
                }

                if (ownerId != null) {
                    parameters.Add("owner_id", ownerId);
                }

                if (assessKey != null) {
                    parameters.Add("access_key", assessKey);
                }

                string? json = await apiInvoke.InvokeAsync("audio.get", parameters);

                _logger.LogDebug("RESULT OF 'audio.get'" + json);


                ResponseData model = JsonConvert.DeserializeObject<ResponseData>(json)!;

                _logger.LogInformation("Successful invoke 'audio.get' ");

                return model.Items;
                //vkApi.Audio.GetAsync(new VkNet.Model.RequestParams.AudioGetParams() { })
            }catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<ResponseData> ReplaceBlockAsync(string replaceId)
        {
            try
            {
                _logger.LogInformation($"invoke 'catalog.replaceBlocks' with replaceId = {replaceId} ");

                VkParameters parameters = new VkParameters
                {
                    
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"replacement_ids", replaceId},
                };



                string? json = await apiInvoke.InvokeAsync("catalog.replaceBlocks", parameters);

                _logger.LogDebug("RESULT OF 'catalog.replaceBlocks'" + json);


                ResponseData? model = JsonConvert.DeserializeObject<ResponseData>(json);

                _logger.LogInformation("Successful invoke 'catalog.replaceBlocks' ");


                return model!;
            }catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task StatsTrackEvents(List<TrackEvent> obj)
        {
            try
            {
                _logger.LogInformation($"invoke 'stats.trackEvents' ");

                string stats = JsonConvert.SerializeObject(obj);
                VkParameters parameters = new VkParameters
                {
                    
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"events", stats},
                };

                Debug.WriteLine($"SEND stats.trackEvents : {stats}");

                string? json = await apiInvoke.InvokeAsync("stats.trackEvents", parameters);

                _logger.LogInformation("Successful invoke 'stats.trackEvents' ");
            }catch(System.Exception ex)
            {
                Debug.Fail(ex.Message, ex.StackTrace);
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task FollowCurator(long curatorId)
        {
            try
            {
                _logger.LogInformation($"invoke 'audio.followCurator' with curatorId = {curatorId} ");

                VkParameters parameters = new VkParameters
                {
                    
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"curator_id", curatorId},
                };


                string? json = await apiInvoke.InvokeAsync("audio.followCurator", parameters);

                _logger.LogDebug("RESULT OF 'audio.followCurator'" + json);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task UnfollowCurator(long curatorId)
        {
            try
            {
                _logger.LogInformation($"invoke 'audio.unfollowCurator' with curatorId = {curatorId} ");

                VkParameters parameters = new VkParameters
                {
                    
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"curator_id", curatorId},
                };


                string? json = await apiInvoke.InvokeAsync("audio.unfollowCurator", parameters);

                _logger.LogDebug("RESULT OF 'audio.unfollowCurator'" + json);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }

        }

        public async Task FollowArtist(string artistId, string referenceId)
        {
            try
            {
                _logger.LogInformation($"invoke 'audio.followArtist' with curatorId = {artistId} ");

                VkParameters parameters = new VkParameters
                {
                    
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"artist_id", artistId},
                    {"ref", referenceId},
                };


                string? json = await apiInvoke.InvokeAsync("audio.followArtist", parameters);

                _logger.LogDebug("RESULT OF 'audio.followArtist'" + json);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task UnfollowArtist(string artistId, string referenceId)
        {
            try
            {
                _logger.LogInformation($"invoke 'audio.unfollowArtist' with curatorId = {artistId} ");

                VkParameters parameters = new VkParameters
                {
                    
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"artist_id", artistId},
                    {"ref", referenceId},
                };


                string? json = await apiInvoke.InvokeAsync("audio.unfollowArtist", parameters);

                _logger.LogDebug("RESULT OF 'audio.unfollowArtist'" + json);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }

        }

        public async Task<RestrictionPopupData> AudioGetRestrictionPopup(string trackCode, string audio)
        {
            try
            {
                _logger.LogInformation($"invoke 'audio.getRestrictionPopup' with trackCode= {trackCode} and audio = {audio} ");

                VkParameters parameters = new VkParameters
                {
                    
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"track_code", trackCode},
                    {"audio_id", audio }
                };


                string? json = await apiInvoke.InvokeAsync("audio.getRestrictionPopup", parameters);

                RestrictionPopupData? model = JsonConvert.DeserializeObject<RestrictionPopupData>(json);


                _logger.LogDebug("RESULT OF 'audio.getRestrictionPopup'" + json);

                return model!;
            }
            catch (System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }

        }

        public async Task<ResponseData> GetPodcastsAsync(string? url = null)
        {
            try
            {
                _logger.LogInformation($"Invoke 'catalog.getPodcasts' with curatorId ");

                VkParameters parameters = new VkParameters
                {
                    
                    {"extended", "1"},
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"url", url},
                };


                string? json = await apiInvoke.InvokeAsync("catalog.getPodcasts", parameters);
                _logger.LogDebug("RESULT OF 'catalog.getPodcasts'" + json);


                ResponseData? model = JsonConvert.DeserializeObject<ResponseData>(json);

                _logger.LogInformation("Successful invoke 'catalog.getAudioCurator' ");


                return model!;
            }
            catch (System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<ResponseData> SectionHomeAsync()
        {
            string code = "var catalogs = API.catalog.getAudio({\"need_blocks\": 0}).catalog.sections;return API.catalog.getSection({\"need_blocks\": 1, \"section_id\": catalogs[0].id});";

            VkParameters parameters = new VkParameters
                {
                    
                    {"extended", "1"},
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"code", code},
                };


            string? json = await apiInvoke.InvokeAsync("execute", parameters);


            ResponseData? model = JsonConvert.DeserializeObject<ResponseData>(json);

            _logger.LogDebug("RESULT OF 'execute'" + json);

            return model!;
        }

        public async Task<ResponseData> GetRecommendationsAudio(string audio)
        {
            try
            {
                VkParameters parameters = new VkParameters
                {
                    
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"target_audio", audio},
                };


                string? json = await apiInvoke.InvokeAsync("audio.getRecommendations", parameters);


                ResponseData? model = JsonConvert.DeserializeObject<ResponseData>(json);

                return model!;
            }catch (System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
          
        }

        public async Task SetBroadcastAsync(Audio? audio)
        {
            try
            {
                if (audio is null)
                {
                    await vkApi.Audio.SetBroadcastAsync();
                     return;
                }

                await vkApi.Audio.SetBroadcastAsync(audio.OwnerId + "_" + audio.Id + "_" + audio.AccessKey);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<List<Playlist>> GetPlaylistsAsync(long ownerId)
        {
            try
            {
                VkParameters parameters = new VkParameters
                {
                    
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"owner_id", ownerId},
                    {"count", 100}

                };


                string? json = await apiInvoke.InvokeAsync("audio.getPlaylists", parameters);


                var model = JsonConvert.DeserializeObject<OldResponseData<Playlist>>(json);

                return model!.Items;
            }
            catch (System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }


        public async Task AddToPlaylistAsync(Audio audio, long ownerId, long playlistId)
        {
            try
            {
                string audioId = audio.OwnerId + "_" + audio.Id;
                await vkApi.Audio.AddToPlaylistAsync(ownerId, playlistId, new List<string>() { audioId });
            }catch(System.Exception ex)
             {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
         }

        public async Task<long> CreatePlaylistAsync(long ownerId, string title, string description, IEnumerable<Audio> tracks)
        {
            try
            {
                var audios = tracks.Select(t => t.OwnerId + "_" + t.Id).ToList();

                AudioPlaylist? result = await vkApi.Audio.CreatePlaylistAsync(ownerId, title, description, audios);

                return result.Id!.Value;
            }catch (System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task SetPlaylistCoverAsync(long ownerId, long playlistId, string hash, string photo)
        {
            try
            {
                VkParameters parameters = new VkParameters
                {
                    
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"owner_id", ownerId},
                    {"playlist_id", playlistId},
                    {"hash", hash},
                    {"photo", photo}
                };


                string? json = await apiInvoke.InvokeAsync("audio.setPlaylistCoverPhoto", parameters);

            }
            catch (System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<UploadPlaylistCoverServerResult> GetPlaylistCoverUploadServerAsync(long ownerId, long playlistId)
        {
            try
            {
                VkParameters parameters = new VkParameters
                {
                    
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"owner_id", ownerId},
                    {"playlist_id", playlistId},
                };

                string? json = await apiInvoke.InvokeAsync("photos.getAudioPlaylistCoverUploadServer", parameters);

                var model = JsonConvert.DeserializeObject<UploadPlaylistCoverServerResult>(json);

                return model!;
            }
            catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }   
        }

        public async Task<UploadPlaylistCoverResult> UploadPlaylistCoverAsync(string uploadUrl, string path)
        {
            using HttpClient httpClient = new HttpClient();
            using (FileStream stream = File.OpenRead(path))
            {
                MultipartFormDataContent content = new MultipartFormDataContent();
                StreamContent streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                content.Add(streamContent, "photo", Path.GetFileName(path));

                HttpResponseMessage response = await httpClient.PostAsync(uploadUrl, content).ConfigureAwait(false);

                string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                UploadPlaylistCoverResult? data = JsonConvert.DeserializeObject<UploadPlaylistCoverResult>(result);

                return data!;

            }
        }

        public async Task EditPlaylistAsync(long ownerId, int playlistId, string title, string description, List<Audio> tracks)
        {
            try
            {
                var audios = tracks.Select(t => t.OwnerId + "_" + t.Id);

                bool result = await vkApi.Audio.EditPlaylistAsync(ownerId, playlistId, title, description, audios);

            }
            catch (System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<BoomToken?> GetBoomToken()
        {
            Guid uuid = Guid.NewGuid();

            try
            {
                VkParameters parameters = new VkParameters
                {

                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    {"app_id", 6767438 },
                    {"app_id", 6767438 },
                    {"timestamp", DateTimeOffset.Now.ToUnixTimeSeconds() },
                    {"app_secret", "ppBOmwQYYOMGulmaiPyK" },
                    {"package", "com.uma.musicvk" },
                    {"uuid", uuid.ToString() },
                    {"digest_hash", "2D0D1nXbs2cX1/Q8wFkyv93NHts="}
                };

                string? json = await apiInvoke.InvokeAsync("auth.getCredentialsForService", parameters);

                var result = JsonConvert.DeserializeObject<List<BoomToken>>(json);

                BoomToken? model = result!.FirstOrDefault();

                if(model != null)
                {
                    model.Uuid = uuid.ToString();
                }

                return model;
            }
            catch (System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task FollowOwner(long ownerId)
        {
            try
            {
                _logger.LogInformation($"Invoke 'audio.followOwner' with ownerId ");
                VkParameters parameters = new VkParameters
                {
                    
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"owner_id", ownerId},
                };

                string? json = await apiInvoke.InvokeAsync("audio.followOwner", parameters);
                _logger.LogDebug("RESULT OF 'audio.followOwner'" + json);
                
                _logger.LogInformation("Successful invoke 'audio.followOwner' ");
            }
            catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }   
        }
        
        public async Task UnfollowOwner(long ownerId)
        {
            try
            {
                _logger.LogInformation($"Invoke 'audio.unfollowOwner' with ownerId ");
                VkParameters parameters = new VkParameters
                {
                    
                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},
                    
                    {"owner_id", ownerId},
                };

                string? json = await apiInvoke.InvokeAsync("audio.unfollowOwner", parameters);
                _logger.LogDebug("RESULT OF 'audio.unfollowOwner'" + json);
                
                _logger.LogInformation("Successful invoke 'audio.unfollowOwner' ");
            }
            catch(System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }   
        }

        public async Task<Lyrics> GetLyrics(string audioId)
        {
            try
            {
                _logger.LogInformation($"Invoke 'audio.getLyrics' with audioId  {audioId}");
                VkParameters parameters = new VkParameters
                {

                    {"device_id", await _deviceIdStore.GetDeviceIdAsync()},

                    {"audio_id", audioId},
                };

                string? json = await apiInvoke.InvokeAsync("audio.getLyrics", parameters);
                _logger.LogDebug("RESULT OF 'audio.getLyrics'" + json);

                Lyrics? model = JsonConvert.DeserializeObject<Lyrics>(json);

                _logger.LogInformation("Successful invoke 'audio.getLyrics' ");

                return model!;
            }
            catch (System.Exception ex)
            {
                _logger.LogError("VK API ERROR:");
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}
