using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Cover
    {
        [JsonProperty("sizes")]
        public List<Image> Sizes { get; set; } = null!;
    }
}
