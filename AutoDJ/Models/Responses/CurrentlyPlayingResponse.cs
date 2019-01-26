using AutoDJ.Models.Spotify;
using Newtonsoft.Json;

namespace AutoDJ.Models.Responses
{
    public class CurrentlyPlayingResponse
    {
        [JsonProperty("progress_ms")]
        public int ProgressMilliseconds { get; set; }

        [JsonProperty("item")]
        public Track Item { get; set; }
    }
}
