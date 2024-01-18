using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Icon
    {
        [JsonProperty("url")]
        public string Url { get; set; } = null!;

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }
    }

    public class RestrictionPopupData
    {
        [JsonProperty("title")]
        public string Title { get; set; } = null!;

        [JsonProperty("icons")]
        public List<Icon> Icons { get; set; } = null!;

        [JsonProperty("text")]
        public string Text { get; set; } = null!;
    }

}
