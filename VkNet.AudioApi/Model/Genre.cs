using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Genre
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = null!;
    }

}
