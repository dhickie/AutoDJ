using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoDJ.Models.Spotify;
using AutoDJ.Options;
using Microsoft.Extensions.Options;

namespace AutoDJ.Services
{
    public class ComponentPlaylistTrackingService : IComponentPlaylistTrackingService
    {
        private readonly IPersistenceService _persistenceService;
        private readonly ISpotifyService _spotifyService;

        private string _bangerPlaylistId;
        private string _fillerPlaylistId;
        private string _playbackPlaylistId;
        private readonly SemaphoreSlim _initLock;

        private List<Track> _bangerPlaylist;
        private List<Track> _fillerPlaylist;
        private int _lastBangerPlayedIndex;
        private int _lastFillerPlayedIndex;

        public ComponentPlaylistTrackingService(IPersistenceService persistenceService, ISpotifyService spotifyService, IOptions<SpotifyOptions> options)
        {
            _persistenceService = persistenceService;
            _spotifyService = spotifyService;

            _bangerPlaylistId = options.Value.BangerPlaylistId;
            _fillerPlaylistId = options.Value.FillerPlaylistId;
            _playbackPlaylistId = options.Value.PlaybackPlaylistId;

            _initLock = new SemaphoreSlim(1, 1);
            _lastBangerPlayedIndex = -1;
            _lastFillerPlayedIndex = -1;
        }

        public async Task<int> GetLastBangerIndex()
        {
            await InitialiseDataIfRequired();
            return _lastBangerPlayedIndex;
        }

        public async Task<int> GetLastFillerIndex()
        {
            await InitialiseDataIfRequired();
            return _lastFillerPlayedIndex;
        }

        public async Task PopulateLastTrackIndexes()
        {
            // Get the content of the playback playlist
            var playbackPlaylist = (await _spotifyService.GetPlaylistContent(_playbackPlaylistId)).ToList();

            // Also get the track that's currently playing
            var currentTrack = await _spotifyService.GetCurrentTrack();

            // Figure out the position the current track has in the current playlist
            var currentIndex = playbackPlaylist.FindIndex(t => t.Id == currentTrack.Id);

            // For each playlist, figure out the last song from each that was played in the playback playlist (if any)
            var lastBangerIndex = FindLastTrackIndexFromPlaylist(currentIndex, _bangerPlaylist, playbackPlaylist);
            if (lastBangerIndex >= 0)
            {
                _lastBangerPlayedIndex = lastBangerIndex;
            }

            var lastFillerIndex = FindLastTrackIndexFromPlaylist(currentIndex, _fillerPlaylist, playbackPlaylist);
            if (lastFillerIndex >= 0)
            {
                _lastFillerPlayedIndex = lastFillerIndex;
            }

            await _persistenceService.SaveIndexes(_lastBangerPlayedIndex, _lastFillerPlayedIndex);
        }

        private int FindLastTrackIndexFromPlaylist(int currentIndex, List<Track> sourcePlaylist, List<Track> playbackPlaylist)
        {
            var lastPlaybackIndex = playbackPlaylist.FindLastIndex(currentIndex, x => sourcePlaylist.Any(t => t.Id == x.Id));
            if (lastPlaybackIndex < 0)
            {
                return lastPlaybackIndex;
            }
            else
            {
                return sourcePlaylist.FindIndex(x => x.Id == playbackPlaylist[lastPlaybackIndex].Id);
            }
        }

        private async Task InitialiseDataIfRequired()
        {
            if (_bangerPlaylist == null || _fillerPlaylist == null)
            {
                await _initLock.WaitAsync();

                try
                {
                    if (_bangerPlaylist == null || _fillerPlaylist == null)
                    {
                        var playlistTask = InitialisePlaylists();
                        var indexTask = InitialiseIndexes();

                        await Task.WhenAll(playlistTask, indexTask);
                    }
                }
                finally
                {
                    _initLock.Release();
                }
            }
        }

        private async Task InitialisePlaylists()
        {
            var bangerPlaylistTask = _spotifyService.GetPlaylistContent(_bangerPlaylistId);
            var fillerPlaylistTask = _spotifyService.GetPlaylistContent(_fillerPlaylistId);

            _bangerPlaylist = (await bangerPlaylistTask).ToList();
            _fillerPlaylist = (await fillerPlaylistTask).ToList();
        }

        private async Task InitialiseIndexes()
        {
            var indexes = await _persistenceService.GetIndexes();

            _lastBangerPlayedIndex = indexes.Item1;
            _lastFillerPlayedIndex = indexes.Item2;
        }
    }
}
