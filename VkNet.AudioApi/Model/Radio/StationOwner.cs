using System.Text.Json.Serialization;

namespace VkNet.AudioApi.Model.Radio
{
    public class StationOwner
    {
        [JsonPropertyName("vkId")]
        public long VkId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("photo")]
        public string Photo { get; set; } = null!;

        [JsonPropertyName("ownerCategory")]
        public OwnerCategory OwnerCategory { get; set; }
    }
}
