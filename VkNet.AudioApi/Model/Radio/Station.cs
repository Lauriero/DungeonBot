using System.Text.Json.Serialization;

using ProtoBuf;

namespace VkNet.AudioApi.Model.Radio
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public class Station
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = null!;

        [JsonPropertyName("title")]
        public string Title { get; set; } = null!;

        [JsonPropertyName("cover")]
        public string Cover { get; set; } = null!;

        [JsonPropertyName("description")]
        public string Description { get; set; } = null!;

        [JsonPropertyName("owner")]
        public StationOwner Owner { get; set; } = null!;
    }
}
