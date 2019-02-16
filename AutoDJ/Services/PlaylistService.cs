using AutoDJ.Models.Spotify;
using AutoDJ.Options;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDJ.Services
{
    public class PlaylistService : IPlaylistService
    {
        private readonly ISpotifyService _spotifyService;
        private readonly string _bangerPlaylistId;
        private readonly string _fillerPlaylistId;

        private List<Track> _bangerPlaylist;
        private List<Track> _fillerPlaylist;
        private SemaphoreSlim _initLock;

        public PlaylistService(ISpotifyService spotifyService, IOptions<SpotifyOptions> options)
        {
            _spotifyService = spotifyService;
            _bangerPlaylistId = options.Value.BangerPlaylistId;
            _fillerPlaylistId = options.Value.FillerPlaylistId;

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

                        await Task.WhenAll(bangerTask, fillerTask);
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
