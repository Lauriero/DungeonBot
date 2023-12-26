using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class RecommendedPlaylist
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("owner_id")]
        public long OwnerId { get; set; }

        [JsonProperty("percentage")]
        public string Percentage { get; set; } = null!;

        [JsonProperty("percentage_title")]
        public string PercentageTitle { get; set; } = null!;

        [JsonProperty("audios")]
        public List<string> AudiosIds { get; set; } = null!;

        [JsonProperty("color")]
        public string Color { get; set; } = null!;

        [JsonProperty("cover")]
        public string Cover { get; set; } = null!;

        public Playlist Playlist { get; set; } = null!;

        public List<Audio> Audios { get; set; } = new List<Audio>();
    }
}
