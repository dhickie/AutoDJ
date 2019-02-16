using AutoDJ.Models.Spotify;
using AutoDJ.Options;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDJ.Services
{
    public class PlaylistService : IPlaylistService
    {
        private readonly ISpotifyService _spotifyService;
        private readonly string _bangerPlaylistId;
        private readonly string _fillerPlaylistId;
        private readonly string _playbackPlaylistId;

        private List<Track> _bangerPlaylist;
        private List<Track> _fillerPlaylist;
        private SemaphoreSlim _initLock;

        public PlaylistService(ISpotifyService spotifyService, IOptions<SpotifyOptions> options)
        {
            _spotifyService = spotifyService;
            _bangerPlaylistId = options.Value.BangerPlaylistId;
            _fillerPlaylistId = options.Value.FillerPlaylistId;
            _playbackPlaylistId = options.Value.PlaybackPlaylistId;

            _initLock = new SemaphoreSlim(1, 1);
            _bangerPlaylist = new List<Track>();
            _fillerPlaylist = new List<Track>();
        }

        public async Task<List<Track>> GetBangerPlaylist()
        {
            await InitialisePlaylistsIfRequired();
            return _bangerPlaylist;
        }

        public async Task<List<Track>> GetFillerPlaylist()
        {
            await InitialisePlaylistsIfRequired();
            return _fillerPlaylist;
        }

        public async Task<List<Track>> GetPlaybackPlaylist()
        {
            return (await _spotifyService.GetPlaylistContent(_playbackPlaylistId)).ToList();
        }

        private async Task InitialisePlaylistsIfRequired()
        {
            if (_bangerPlaylist == null || _fillerPlaylist == null)
            {
                await _initLock.WaitAsync();

                try
                {
                    if (_bangerPlaylist == null || _fillerPlaylist == null)
                    {
                        var bangerTask = _spotifyService.GetPlaylistContent(_bangerPlaylistId);
                        var fillerTask = _spotifyService.GetPlaylistContent(_fillerPlaylistId);

                        _bangerPlaylist = (await bangerTask).ToList();
                        _fillerPlaylist = (await fillerTask).ToList();
                    }
                }
                finally
                {
                    _initLock.Release();
                }
            }
        }
    }
}
