using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Catalog
    {
        [JsonProperty("default_section")]
        public string DefaultSection { get; set; } = null!;

        [JsonProperty("sections")]
        public List<Section> Sections { get; set; } = null!;
    }
}
