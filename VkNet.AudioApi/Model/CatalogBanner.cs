using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class CatalogBanner
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("click_action")]
        public ClickAction ClickAction { get; set; } = null!;

        [JsonProperty("buttons")]
        public List<Button> Buttons { get; set; } = null!;

        [JsonProperty("images")]
        public List<Image> Images { get; set; } = null!;

        [JsonProperty("text")]
        public string Text { get; set; } = null!;

        [JsonProperty("subtext")]
        public string SubText { get; set; } = null!;

        [JsonProperty("title")]
        public string Title { get; set; } = null!;

        [JsonProperty("track_code")]
        public string TrackCode { get; set; } = null!;

        [JsonProperty("image_mode")]
        public string ImageMode { get; set; } = null!;

    }

    public class ClickAction
    {
        [JsonProperty("action")]
        public ActionButton Action { get; set; } = null!;
    }
}
