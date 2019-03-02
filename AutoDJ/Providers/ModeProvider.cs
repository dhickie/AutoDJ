using AutoDJ.Models.Spotify;
using AutoDJ.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoDJ.Providers
{
    public class ModeProvider : IModeProvider
    {
        private readonly ISpotifyService _spotifyService;
        private readonly IPlaylistService _playlistService;
        private readonly IPlaylistTrackingService _componentPlaylistTrackingService;
        private readonly IPersistenceService _persistenceService;

        private List<Track> _bangerPlaylist;
        private List<Track> _fillerPlaylist;
        private List<Track> _endOfNightPlaylist;

        public ModeProvider(ISpotifyService spotifyService, 
            IPlaylistService playlistService, 
            IPlaylistTrackingService trackingService, 
            IPersistenceService persistenceService)
        {
            _spotifyService = spotifyService;
            _playlistService = playlistService;
            _persistenceService = persistenceService;
            _componentPlaylistTrackingService = trackingService;
        }

        public async Task SetMode(int modeId)
        {
            var currentMode = await _persistenceService.GetMode();

            if (modeId != currentMode)
            {
                var playlistTask = InitialisePlaylists();
                var indexTask = _componentPlaylistTrackingService.PopulateLastTrackIndexes();
                await Task.WhenAll(playlistTask, indexTask);

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
                    case 3:
                        await SetEndOfNightMode();
                        break;
                }

                await _persistenceService.SaveMode(modeId);
            }
        }

        private async Task InitialisePlaylists()
        {
            var bangerPlaylistTask = _playlistService.GetBangerPlaylist();
            var fillerPlaylistTask = _playlistService.GetFillerPlaylist();
            var endOfNightPlaylistTask = _playlistService.GetEndOfNightPlaylist();

            _bangerPlaylist = await bangerPlaylistTask;
            _fillerPlaylist = await fillerPlaylistTask;
            _endOfNightPlaylist = await endOfNightPlaylistTask;
        }

        private async Task SetFullThrottleMode()
        {
            // Nothing but bangers
            var trackIds = new List<string>();
            var lastBangerIndex = await _componentPlaylistTrackingService.GetLastBangerIndex();

            for (var i = lastBangerIndex + 1; i < _bangerPlaylist.Count; i++)
            {
                trackIds.Add(_bangerPlaylist[i].Id);
            }

            await _spotifyService.SetPlaylistContent(trackIds);
        }

        private async Task SetNormalMode()
        {
            // 2 bangers followed by 1 filler
            var trackIds = new List<string>();
            var lastBangerIndex = await _componentPlaylistTrackingService.GetLastBangerIndex();
            var lastFillerIndex = await _componentPlaylistTrackingService.GetLastFillerIndex();

            // Ensure that we don't interrupt a banger pairing if we're in the middle of one from Full Throttle mode
            if (lastBangerIndex % 2 != 0 && _bangerPlaylist.Count > lastBangerIndex + 1)
            {
                trackIds.Add(_bangerPlaylist[lastBangerIndex + 1].Id);
                lastBangerIndex++;
            }

            while (lastBangerIndex < _bangerPlaylist.Count - 2 || lastFillerIndex < _fillerPlaylist.Count - 1)
            {
                if (lastBangerIndex < _bangerPlaylist.Count - 2)
                {
                    trackIds.Add(_bangerPlaylist[lastBangerIndex + 1].Id);
                    trackIds.Add(_bangerPlaylist[lastBangerIndex + 2].Id);
                    lastBangerIndex += 2;
                }

                if (lastFillerIndex < _fillerPlaylist.Count - 1)
                {
                    trackIds.Add(_fillerPlaylist[lastFillerIndex + 1].Id);
                    lastFillerIndex++;
                }
            }

            await _spotifyService.SetPlaylistContent(trackIds);
        }

        private async Task SetFillerMode()
        {
            // Nothing but fillers
            var trackIds = new List<string>();

            // We need to know the last banger index because if we're 1 song through a "pairing" then we need the next song as well
            var lastBangerIndex = await _componentPlaylistTrackingService.GetLastBangerIndex();
            var lastFillerIndex = await _componentPlaylistTrackingService.GetLastFillerIndex();

            if (lastBangerIndex % 2 != 0 && _bangerPlaylist.Count > lastBangerIndex + 1)
            {
                trackIds.Add(_bangerPlaylist[lastBangerIndex + 1].Id);
            }

            for (var i = lastFillerIndex + 1; i < _fillerPlaylist.Count; i++)
            {
                trackIds.Add(_fillerPlaylist[i].Id);
            }

            await _spotifyService.SetPlaylistContent(trackIds);
        }

        private async Task SetEndOfNightMode()
        {
            // Just the end of night playlist
            var trackIds = _endOfNightPlaylist.Select(t => t.Id).ToList();

            await _spotifyService.SetPlaylistContent(trackIds);
        }
    }
}
