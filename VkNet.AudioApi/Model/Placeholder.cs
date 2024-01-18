using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Placeholder
    {
        [JsonProperty("title")]
        public string Title { get; set; } = null!;

        [JsonProperty("id")]
        public string Id { get; set; } = null!;

        [JsonProperty("icons")]
        public List<Image> Icons { get; set; } = new List<Image>();

        [JsonProperty("text")]
        public string Text { get; set; } = null!;

        [JsonProperty("buttons")]
        public List<Button> Buttons { get; set; } = new List<Button>();
    }
}
