using Newtonsoft.Json;

namespace VkNet.AudioApi.Model.General
{
    public class ErrorVk
    {
        [JsonProperty("error_code")]
        public int ErrorCode { get; set; }

        [JsonProperty("error_msg")]
        public string ErrorMsg { get; set; } = null!;

        [JsonProperty("request_params")]
        public List<RequestParam> RequestParams { get; set; } = null!;
    }

    public class RequestParam
    {
        [JsonProperty("key")]
        public string Key { get; set; } = null!;

        [JsonProperty("value")]
        public string Value { get; set; } = null!;
    }
}
