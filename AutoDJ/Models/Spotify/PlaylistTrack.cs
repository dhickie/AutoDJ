using Newtonsoft.Json;

namespace AutoDJ.Models.Spotify
{
    public class PlaylistTrack
    {
        [JsonProperty("track")]
        public Track Track { get; set; }
    }
}
