using AutoDJ.Models.Spotify;
using AutoDJ.Options;
using AutoDJ.Services;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDJ.Providers
{
    public class ModeProvider : IModeProvider
    {
        private readonly ISpotifyService _spotifyService;
        private readonly SemaphoreSlim _playlistLock;
        private readonly string _bangerPlaylistId;
        private readonly string _fillerPlaylistId;
        private readonly string _playbackPlaylistId;

        private List<Track> _bangerPlaylist;
        private List<Track> _fillerPlaylist;
        private int _lastBangerPlayedIndex;
        private int _lastFillerPlayedIndex;

        public ModeProvider(ISpotifyService spotifyService, IOptions<SpotifyOptions> options)
        {
            _spotifyService = spotifyService;

            _bangerPlaylistId = options.Value.BangerPlaylistId;
            _fillerPlaylistId = options.Value.FillerPlaylistId;
            _playbackPlaylistId = options.Value.PlaybackPlaylistId;

            _playlistLock = new SemaphoreSlim(1, 1);
            _lastBangerPlayedIndex = -1;
            _lastFillerPlayedIndex = -1;
        }

        public async Task SetMode(int modeId)
        {
            await InitialisePlaylistsIfRequired();
            await PopulateLastTrackIndexes();

            switch (modeId)
            {
                case 0:
                    await SetFullThrottleMode();
                    break;
                case 1:
                    await SetNormalMode();
                    break;
                case 2:
                    await SetFillerMode();
                    break;
            }
        }

        private async Task InitialisePlaylistsIfRequired()
        {
            if (_bangerPlaylist == null || _fillerPlaylist == null)
            {
                await _playlistLock.WaitAsync();

                try
                {
                    if (_bangerPlaylist == null || _fillerPlaylist == null)
                    {
                        var bangerPlaylistTask = _spotifyService.GetPlaylistContent(_bangerPlaylistId);
                        var fillerPlaylistTask = _spotifyService.GetPlaylistContent(_fillerPlaylistId);

                        _bangerPlaylist = (await bangerPlaylistTask).ToList();
                        _fillerPlaylist = (await fillerPlaylistTask).ToList();
                    }
                }
                finally
                {
                    _playlistLock.Release();
                }
            }
        }

        private async Task PopulateLastTrackIndexes()
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
        }

        private int FindLastTrackIndexFromPlaylist(int currentIndex, List<Track> sourcePlaylist, List<Track> playbackPlaylist)
        {
            var lastPlaybackIndex = playbackPlaylist.FindLastIndex(currentIndex, x => sourcePlaylist.Any(t => t.Id == x.Id)); // currentIndex+1 because the song currently playing will have "been played" by the time the next song comes on
            if (lastPlaybackIndex < 0)
            {
                return lastPlaybackIndex;
            }
            else
            {
                return sourcePlaylist.FindIndex(x => x.Id == playbackPlaylist[lastPlaybackIndex].Id);
            }
        }

        private async Task SetFullThrottleMode()
        {
            // Nothing but bangers
            var trackIds = new List<string>();

            for (var i = _lastBangerPlayedIndex + 1; i < _bangerPlaylist.Count; i++)
            {
                trackIds.Add(_bangerPlaylist[i].Id);
            }

            await _spotifyService.SetPlaylistContent(trackIds);
        }

        private async Task SetNormalMode()
        {
            // 2 bangers followed by 1 filler
            var trackIds = new List<string>();
            var lastBangerIndex = _lastBangerPlayedIndex;
            var lastFillerIndex = _lastFillerPlayedIndex;

            while (lastBangerIndex < _bangerPlaylist.Count - 2 && lastFillerIndex < _fillerPlaylist.Count - 1)
            {
                trackIds.Add(_bangerPlaylist[lastBangerIndex+1].Id);
                trackIds.Add(_bangerPlaylist[lastBangerIndex+2].Id);
                trackIds.Add(_fillerPlaylist[lastFillerIndex+1].Id);

                lastBangerIndex += 2;
                lastFillerIndex++;
            }

            await _spotifyService.SetPlaylistContent(trackIds);
        }

        private async Task SetFillerMode()
        {
            // Nothing but fillers
            var trackIds = new List<string>();

            for (var i = _lastFillerPlayedIndex+1; i < _fillerPlaylist.Count; i++)
            {
                trackIds.Add(_fillerPlaylist[i].Id);
            }

            await _spotifyService.SetPlaylistContent(trackIds);
        }
    }
}
