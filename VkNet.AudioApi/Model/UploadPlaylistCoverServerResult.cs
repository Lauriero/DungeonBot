using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class UploadPlaylistCoverServerResult
    {
        [JsonProperty("response")]
        public UploadPlaylistCoverServer Response { get; set; } = null!;
    }

    public class UploadPlaylistCoverServer
    {
        [JsonProperty("upload_url")]
        public string UploadUrl { get; set; } = null!;
    }
}
