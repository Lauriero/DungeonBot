using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Group
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = null!;

        [JsonProperty("screen_name")]
        public string ScreenName { get; set; } = null!;

        [JsonProperty("is_closed")]
        public int IsClosed { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = null!;

        [JsonProperty("is_admin")]
        public int IsAdmin { get; set; }

        [JsonProperty("is_member")]
        public int IsMember { get; set; }

        [JsonProperty("is_advertiser")]
        public int IsAdvertiser { get; set; }

        [JsonProperty("photo_50")]
        public string Photo50 { get; set; } = null!;

        [JsonProperty("photo_100")]
        public string Photo100 { get; set; } = null!;

        [JsonProperty("photo_200")]
        public string Photo200 { get; set; } = null!;

        [JsonProperty("member_status")]
        public int MemberStatus { get; set; }

        [JsonProperty("verified")]
        public int Verified { get; set; }

        [JsonProperty("members_count")]
        public int MembersCount { get; set; }

        [JsonProperty("activity")]
        public string Activity { get; set; } = null!;

        [JsonProperty("trending")]
        public int Trending { get; set; }
    }
}
