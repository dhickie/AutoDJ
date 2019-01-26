using Newtonsoft.Json;

namespace AutoDJ.Models.Spotify
{
    public class Track
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("duration_ms")]
        public string DurationMilliseconds { get; set; }
    }
}
