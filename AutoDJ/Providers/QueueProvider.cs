using AutoDJ.Options;
using AutoDJ.Services;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoDJ.Providers
{
    public class QueueProvider : IQueueProvider
    {
        private readonly IPlaylistService _playlistService;
        private readonly IPlaylistTrackingService _playlistTrackingService;
        private readonly ISpotifyService _spotifyService;

        private readonly Dictionary<string, string> _specialSongMap;

        public QueueProvider(IPlaylistService playlistService, 
            IPlaylistTrackingService playlistTrackingService, 
            ISpotifyService spotifyService, 
            IOptions<SpotifyOptions> options)
        {
            _playlistService = playlistService;
            _playlistTrackingService = playlistTrackingService;
            _spotifyService = spotifyService;

            _specialSongMap = options.Value.SpecialSongs;
        }

        public async Task AddSong(string songKey)
        {
            await _playlistTrackingService.PopulateLastTrackIndexes();

            var songId = GetSongId(songKey);
            var playbackPlaylist = await _playlistService.GetPlaybackPlaylist();

            // Insert the requested song next in the playlist
            var currentIndex = await _playlistTrackingService.GetCurrentPlaybackPlaylistIndex(playbackPlaylist);

            var truncatedPlaylist = playbackPlaylist.Skip(currentIndex).Select(t => t.Id).ToList();
            truncatedPlaylist.Insert(1, songId);

            await _spotifyService.SetPlaylistContent(truncatedPlaylist);
        }

        private string GetSongId(string songKey)
        {
            if (!_specialSongMap.ContainsKey(songKey))
            {
                throw new KeyNotFoundException($"Unable to find song with key {songKey}");
            }

            return _specialSongMap[songKey];
        }
    }
}
