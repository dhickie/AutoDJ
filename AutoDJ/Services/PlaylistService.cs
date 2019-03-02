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
        private readonly string _endOfNightPlaylistId;
        private readonly string _playbackPlaylistId;

        private List<Track> _bangerPlaylist;
        private List<Track> _fillerPlaylist;
        private List<Track> _endOfNightPlaylist;
        private SemaphoreSlim _initLock;

        public PlaylistService(ISpotifyService spotifyService, IOptions<SpotifyOptions> options)
        {
            _spotifyService = spotifyService;
            _bangerPlaylistId = options.Value.BangerPlaylistId;
            _fillerPlaylistId = options.Value.FillerPlaylistId;
            _endOfNightPlaylistId = options.Value.EndOfNightPlaylistId;
            _playbackPlaylistId = options.Value.PlaybackPlaylistId;

            _initLock = new SemaphoreSlim(1, 1);
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

        public async Task<List<Track>> GetEndOfNightPlaylist()
        {
            await InitialisePlaylistsIfRequired();
            return _endOfNightPlaylist;
        }

        public async Task<List<Track>> GetPlaybackPlaylist()
        {
            return (await _spotifyService.GetPlaylistContent(_playbackPlaylistId)).ToList();
        }

        private async Task InitialisePlaylistsIfRequired()
        {
            if (_bangerPlaylist == null || _fillerPlaylist == null || _endOfNightPlaylist == null)
            {
                await _initLock.WaitAsync();

                try
                {
                    if (_bangerPlaylist == null || _fillerPlaylist == null || _endOfNightPlaylist == null)
                    {
                        var bangerTask = _spotifyService.GetPlaylistContent(_bangerPlaylistId);
                        var fillerTask = _spotifyService.GetPlaylistContent(_fillerPlaylistId);
                        var endOfNightTask = _spotifyService.GetPlaylistContent(_endOfNightPlaylistId);

                        _bangerPlaylist = (await bangerTask).ToList();
                        _fillerPlaylist = (await fillerTask).ToList();
                        _endOfNightPlaylist = (await endOfNightTask).ToList();
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
