using AutoDJ.Models.Spotify;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoDJ.Services
{
    public interface ISpotifyService
    {
        Task<Track> GetCurrentTrack();
        Task SetPlaylistContent(ICollection<string> trackIds);
        Task<ICollection<Track>> GetPlaylistContent(string playlistId);
    }
}
