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
        private int _nextBangerTrackIndex;
        private int _nextFillerTrackIndex;

        public ModeProvider(ISpotifyService spotifyService, IOptions<SpotifyOptions> options)
        {
            _playlistLock = new SemaphoreSlim(1, 1);
            _bangerPlaylistId = options.Value.BangerPlaylistId;
            _fillerPlaylistId = options.Value.FillerPlaylistId;

            _spotifyService = spotifyService;
        }

        public async Task SetMode(int modeId)
        {
            await InitialisePlaylistsIfRequired();
            await PopulateNextTrackIndexes();

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

        private async Task PopulateNextTrackIndexes()
        {
            // Get the content of the playback playlist
            var playbackPlaylist = (await _spotifyService.GetPlaylistContent(_playbackPlaylistId)).ToList();

            // Also get the track that's currently playing
            var currentTrack = await _spotifyService.GetCurrentTrack();

            // Figure out the position the current track has in the current playlist
            var currentIndex = playbackPlaylist.FindIndex(t => t.Id == currentTrack.Id);

            // From there, figure out what the next IDs from the filler and banger playlists are
            _nextBangerTrackIndex = FindNextTrackIndexFromPlaylist(currentIndex + 1, _bangerPlaylist, playbackPlaylist);
            _nextFillerTrackIndex = FindNextTrackIndexFromPlaylist(currentIndex + 1, _fillerPlaylist, playbackPlaylist);
        }

        private int FindNextTrackIndexFromPlaylist(int startIndex, List<Track> sourcePlaylist, List<Track> playbackPlaylist)
        {
            return playbackPlaylist.FindIndex(startIndex, x => sourcePlaylist.Any(t => t.Id == x.Id));
        }

        private async Task SetFullThrottleMode()
        {
            // Nothing but bangers
            var trackIds = new List<string>();

            for (var i = _nextBangerTrackIndex; i < _bangerPlaylist.Count; i++)
            {
                trackIds.Add(_bangerPlaylist[i].Id);
            }

            await _spotifyService.SetPlaylistContent(trackIds);
        }

        private async Task SetNormalMode()
        {
            // 2 bangers followed by 1 filler
            var trackIds = new List<string>();
            var nextBangerIndex = _nextBangerTrackIndex;
            var nextFillerIndex = _nextFillerTrackIndex;

            while (nextBangerIndex < _bangerPlaylist.Count - 1 && nextFillerIndex < _fillerPlaylist.Count)
            {
                trackIds.Add(_bangerPlaylist[nextBangerIndex].Id);
                trackIds.Add(_bangerPlaylist[nextBangerIndex + 1].Id);
                trackIds.Add(_fillerPlaylist[nextFillerIndex].Id);

                nextBangerIndex += 2;
                nextFillerIndex++;
            }

            await _spotifyService.SetPlaylistContent(trackIds);
        }

        private async Task SetFillerMode()
        {
            // Nothing but fillers
            var trackIds = new List<string>();

            for (var i = _nextFillerTrackIndex; i < _fillerPlaylist.Count; i++)
            {
                trackIds.Add(_fillerPlaylist[i].Id);
            }

            await _spotifyService.SetPlaylistContent(trackIds);
        }
    }
}
