﻿using System.Collections.Generic;

namespace AutoDJ.Options
{
    public class SpotifyOptions
    {
        public string SpotifyBaseUrl { get; set; }
        public string AuthorizationCode { get; set; }
        public string RefreshToken { get; set; }
        public string BangerPlaylistId { get; set; }
        public string FillerPlaylistId { get; set; }
        public string EndOfNightPlaylistId { get; set; }
        public string PlaybackPlaylistId { get; set; }
        public Dictionary<string, string> SpecialSongs { get; set; }
    }
}