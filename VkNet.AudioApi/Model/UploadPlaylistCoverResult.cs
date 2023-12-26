using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class UploadPlaylistCoverResult
    {
        [JsonProperty("hash")]
        public string Hash { get; set; } = null!;

        [JsonProperty("photo")]
        public string Photo { get; set; } = null!;
    }
}
