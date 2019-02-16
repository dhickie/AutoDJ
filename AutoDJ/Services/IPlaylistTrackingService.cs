using AutoDJ.Models.Spotify;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoDJ.Services
{
    public interface IPlaylistTrackingService
    {
        Task PopulateLastTrackIndexes();
        Task<int> GetLastBangerIndex();
        Task<int> GetLastFillerIndex();
        Task<int> GetCurrentPlaybackPlaylistIndex(List<Track> playbackPlaylist);
    }
}
