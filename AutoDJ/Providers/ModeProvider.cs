using AutoDJ.Models.Spotify;
using AutoDJ.Options;
using AutoDJ.Services;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoDJ.Providers
{
    public class ModeProvider : IModeProvider
    {
        private readonly ISpotifyService _spotifyService;
        private readonly IPlaylistService _playlistService;
        private readonly IPlaylistTrackingService _componentPlaylistTrackingService;

        private List<Track> _bangerPlaylist;
        private List<Track> _fillerPlaylist;

        public ModeProvider(ISpotifyService spotifyService, IPlaylistService playlistService, IPlaylistTrackingService trackingService, IOptions<SpotifyOptions> options)
        {
            _spotifyService = spotifyService;
            _playlistService = playlistService;
            _componentPlaylistTrackingService = trackingService;
        }

        public async Task SetMode(int modeId)
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
            }
        }

        private async Task InitialisePlaylists()
        {
            var bangerPlaylistTask = _playlistService.GetBangerPlaylist();
            var fillerPlaylistTask = _playlistService.GetFillerPlaylist();

            _bangerPlaylist = await bangerPlaylistTask;
            _fillerPlaylist = await fillerPlaylistTask;
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
            var lastFillerIndex = await _componentPlaylistTrackingService.GetLastFillerIndex();

            for (var i = lastFillerIndex + 1; i < _fillerPlaylist.Count; i++)
            {
                trackIds.Add(_fillerPlaylist[i].Id);
            }

            await _spotifyService.SetPlaylistContent(trackIds);
        }
    }
}
