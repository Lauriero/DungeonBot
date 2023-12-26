using Newtonsoft.Json;

namespace VkNet.AudioApi.Model;

public class MusicOwner
{
    [JsonProperty("id")]
    public string Id { get; set; } = null!;
    
    [JsonProperty("image")]
    public List<Image> Images { get; set; } = null!;
    
    [JsonProperty("subtitle")]
    public string Subtitle { get; set; } = null!;
    
    [JsonProperty("title")]
    public string Title { get; set; } = null!;
    
    [JsonProperty("url")]
    public string Url { get; set; } = null!;
}