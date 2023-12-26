using Newtonsoft.Json;

using VkNet.Model;

namespace VkNet.AudioApi.Model;

public class AudioFollowingsUpdateInfo
{
    [JsonProperty("title")]
    public string Title { get; set; } = null!;
    
    [JsonProperty("id")]
    public string Id { get; set; } = null!;
    
    [JsonProperty("covers")]
    public List<AudioCover> Covers { get; set; } = null!;
}