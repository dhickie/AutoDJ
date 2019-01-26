using Newtonsoft.Json;
using System.Collections.Generic;

namespace AutoDJ.Models.Spotify
{
    public class Page<T>
    {
        [JsonProperty("items")]
        public ICollection<T> Items { get; set; }

        [JsonProperty("next")]
        public string Next { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }
    }
}
