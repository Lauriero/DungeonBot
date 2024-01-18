using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Link
    {
        [JsonProperty("id")]
        public string Id { get; set; } = null!;

        [JsonProperty("image")]
        public List<Image> Image { get; set; } = null!;

        [JsonProperty("meta")]
        public Meta Meta { get; set; } = null!;

        [JsonProperty("subtitle")]
        public string Subtitle { get; set; } = null!;

        [JsonProperty("title")]
        public string Title { get; set; } = null!;

        [JsonProperty("url")]
        public string Url { get; set; } = null!;
    }

    public class Meta
    {
        [JsonProperty("content_type")]
        public string ContentType { get; set; } = null!;

        [JsonProperty("track_code")]
        public string TrackCode { get; set; } = null!;
    }
}
