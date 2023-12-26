using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class TopTitle
    {
        [JsonProperty("icon")]
        public string Icon { get; set; } = null!;

        [JsonProperty("text")]
        public string Text { get; set; } = null!;
    }

}
