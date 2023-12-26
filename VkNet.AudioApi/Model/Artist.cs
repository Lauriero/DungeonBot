using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Artist
    {
        [JsonProperty("name")]
        public string Name { get; set; } = null!;

        [JsonProperty("domain")]
        public string Domain { get; set; } = null!;

        [JsonProperty("id")]
        public string Id { get; set; } = null!;

        [JsonProperty("is_album_cover")]
        public bool IsAlbumCover { get; set; }

        [JsonProperty("photo")]
        public List<Image> Photo { get; set; } = null!;
        
        [JsonProperty("is_followed")]
        public bool IsFollowed { get; set; }
        
        [JsonProperty("can_follow")]
        public bool CanFollow { get; set; }
    }
}