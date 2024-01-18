using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Album
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; } = null!;

        [JsonProperty("owner_id")]
        public long OwnerId { get; set; }

        [JsonProperty("access_key")]
        public string AccessKey { get; set; } = null!;

        [JsonProperty("thumb")]
        public Photo? Thumb { get; set; }

        public string? Cover
        {
            get
            {
                if (Thumb == null) return null;
                if (Thumb.Photo68 != null) return Thumb.Photo68;
                if (Thumb.Photo135 != null) return Thumb.Photo135;
                if (Thumb.Photo270 != null) return Thumb.Photo270;
                if (Thumb.Photo300 != null) return Thumb.Photo300;
                if (Thumb.Photo600 != null) return Thumb.Photo600;

                return Thumb.Photo1200;
            }
        }
    }
}
