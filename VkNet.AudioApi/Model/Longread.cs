using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Longread
    {
        [JsonProperty("access_key")]
        public string AccessKey { get; set; } = null!;

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("is_favorite")]
        public bool IsFavorite { get; set; }

        [JsonProperty("owner_id")]
        public int OwnerId { get; set; }

        [JsonProperty("owner_name")]
        public string OwnerName { get; set; } = null!;

        [JsonProperty("owner_photo")]
        public string OwnerPhoto { get; set; } = null!;

        [JsonProperty("photo")]
        public Cover Photo { get; set; } = null!;

        [JsonProperty("published_date")]
        public int PublishedDate { get; set; }

        [JsonProperty("state")]
        public string State { get; set; } = null!;

        [JsonProperty("subtitle")]
        public string Subtitle { get; set; } = null!;

        [JsonProperty("title")]
        public string Title { get; set; } = null!;

        [JsonProperty("url")]
        public string Url { get; set; } = null!;

        [JsonProperty("view_url")]
        public string ViewUrl { get; set; } = null!;

        [JsonProperty("views")]
        public int Views { get; set; }

        [JsonProperty("shares")]
        public int Shares { get; set; }

        [JsonProperty("can_report")]
        public bool CanReport { get; set; }
    }
}
