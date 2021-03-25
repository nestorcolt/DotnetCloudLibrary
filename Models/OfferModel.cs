using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CloudLibrary.Models
{
    public class OfferModel
    {
        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("offer_id")]
        public string OfferId { get; set; }

        [JsonProperty("validated")]
        public bool Validated { get; set; }

        [JsonProperty("station_id")]
        public string OfferAreaId { get; set; }

        [JsonProperty("offer_time")]
        public long OfferTime { get; set; }

        [JsonProperty("ttl_attr")]
        public long OfferTimeToLive { get; set; }

        [JsonProperty("data")]
        public JObject OfferData { get; set; }
    }
}
