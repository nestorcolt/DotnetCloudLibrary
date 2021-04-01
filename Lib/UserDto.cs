using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CloudLibrary.Lib
{
    [DataContract]
    public class UserDto
    {
        [DataMember(Name = "user_id")]
        public string UserId { get; set; }

        [DataMember(Name = "access_token")]
        public string AccessToken { get; set; }

        [DataMember(Name = "refresh_token")]
        public string RefreshToken { get; set; }

        [DataMember(Name = "last_active")]
        public long LastActive { get; set; }

        [DataMember(Name = "minimum_price")]
        public float MinimumPrice { get; set; }

        [DataMember(Name = "arrival_time")]
        public int ArrivalTime { get; set; }

        [DataMember(Name = "speed")]
        public float Speed { get; set; }

        [DataMember(Name = "search_blocks")]
        public bool SearchBlocks { get; set; }

        [DataMember(Name = "search_schedule")]
        public JToken SearchSchedule { get; set; }

        [DataMember(Name = "areas")]
        public List<string> Areas { get; set; }

        [DataMember(Name = "time_zone")]
        public string TimeZone { get; set; }

        [DataMember(Name = "service_area")]
        public string ServiceAreaHeader { get; set; }
    }

}
