using Newtonsoft.Json;

namespace VkNet.AudioApi.Model.General
{
    public class OldResponseVk<T>
    {
        [JsonProperty("response")]
        public OldResponseData<T> Response { get; set; } = null!;

        [JsonProperty("error")]
        public ErrorVk Error { get; set; } = null!;
    }

    public class OldResponseData<T>
    {
        [JsonProperty("items")]
        public List<T> Items { get; set; } = null!;
    }
}
