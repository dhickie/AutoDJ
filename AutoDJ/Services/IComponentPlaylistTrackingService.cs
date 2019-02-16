using System.Threading.Tasks;

namespace AutoDJ.Services
{
    public interface IComponentPlaylistTrackingService
    {
        Task PopulateLastTrackIndexes();
        Task<int> GetLastBangerIndex();
        Task<int> GetLastFillerIndex();
    }
}
