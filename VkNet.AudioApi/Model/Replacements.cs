using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Replacement
    {
        [JsonProperty("from_block_ids")]
        public List<string> FromBlockIds { get; set; } = null!;

        [JsonProperty("to_blocks")]
        public List<Block> ToBlocks { get; set; } = null!;
    }

    public class Replacements
    {
        [JsonProperty("replacements")]
        public List<Replacement> ReplacementsModels { get; set; } = null!;

        [JsonProperty("new_next_from")]
        public string NewNextFrom { get; set; } = null!;
    }
}
