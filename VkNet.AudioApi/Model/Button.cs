using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Button
    {
        [JsonProperty("action")]
        public ActionButton Action { get; set; } = null!;

        [JsonProperty("section_id")]
        public string SectionId { get; set; } = null!;

        [JsonProperty("title")]
        public string Title { get; set; } = null!;

        [JsonProperty("ref_items_count")]
        public int RefItemsCount { get; set; }

        [JsonProperty("ref_layout_name")]
        public string RefLayoutName { get; set; } = null!;

        [JsonProperty("ref_data_type")]
        public string RefDataType { get; set; } = null!;

        [JsonProperty("block_id")]
        public string BlockId { get; set; } = null!;

        [JsonProperty("options")]
        public List<OptionButton> Options { get; set; } = new List<OptionButton>();
        
        [JsonProperty("artist_id")]
        public string ArtistId { get; set; } = null!;
        
        [JsonProperty("is_following")]
        public bool IsFollowing { get; set; }
        
        [JsonProperty("owner_id")]
        public long OwnerId { get; set; }
    }
}
