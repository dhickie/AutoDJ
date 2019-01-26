using Newtonsoft.Json;
using System.Collections.Generic;

namespace AutoDJ.Models.Requests
{
    public class SetPlaylistContentRequest
    {
        [JsonProperty("uris")]
        public ICollection<string> URIs { get; set; }
    }
}
