using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class PodcastSliderItem
    {
        [JsonProperty("item_id")]
        public string ItemId { get; set; } = null!;

        [JsonProperty("slider_type")]
        public string SliderType { get; set; } = null!;

        [JsonProperty("episode")]
        public PodcastEpisode Episode { get; set; } = null!;
    }
}
