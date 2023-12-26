using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class BoomToken
    {
        [JsonProperty("token")]
        public string Token { get; set; } = null!;

        [JsonProperty("first_name")]
        public string FirstName { get; set; } = null!;

        [JsonProperty("last_name")]
        public string LastName { get; set; } = null!;

        [JsonProperty("ttl")]
        public int Ttl { get; set; }

        [JsonProperty("photo_50")]
        public string Photo50 { get; set; } = null!;

        [JsonProperty("photo_100")]
        public string Photo100 { get; set; } = null!;

        [JsonProperty("photo_200")]
        public string Photo200 { get; set; } = null!;

        [JsonProperty("phone")]
        public string Phone { get; set; } = null!;

        [JsonProperty("weight")]
        public int Weight { get; set; }

        [JsonProperty("user_hash")]
        public string UserHash { get; set; } = null!;

        [JsonProperty("app_service_id")]
        public int AppServiceId { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; } = null!;

        public string Uuid { get; set; } = null!;
    }
}
