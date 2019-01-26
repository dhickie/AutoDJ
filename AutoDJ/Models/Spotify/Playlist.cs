using Newtonsoft.Json;

namespace AutoDJ.Models.Spotify
{
    public class Playlist
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("tracks")]
        public Page<PlaylistTrack> Tracks { get; set; }
    }
}
