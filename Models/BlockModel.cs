using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CloudLibrary.Models
{
    public class BlockModel
    {
        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("block_id")]
        public long BlockId { get; set; }

        [JsonProperty("station_id")]
        public string BlockAreaId { get; set; }

        [JsonProperty("block_time")]
        public long BlockTime { get; set; }

        [JsonProperty("ttl_attr")]
        public long BlockTimeToLive { get; set; }

        [JsonProperty("data")]
        public JObject BlockData { get; set; }
    }
}
