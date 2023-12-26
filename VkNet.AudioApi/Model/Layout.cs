using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Layout
    {
        [JsonProperty("name")]
        public string Name { get; set; } = null!;

        [JsonProperty("owner_id")]
        public int OwnerId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; } = null!;

        [JsonProperty("is_editable")]
        public int? IsEditable { get; set; }

        [JsonProperty("top_title")]
        public TopTitle TopTitle { get; set; } = null!;

        [JsonProperty("subtitle")]
        public string Subtitle { get; set; } = null!;
    }
}
