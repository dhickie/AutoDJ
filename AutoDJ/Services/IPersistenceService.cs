using System.Threading.Tasks;

namespace AutoDJ.Services
{
    public interface IPersistenceService
    {
        Task SaveIndexes(int bangerIndex, int fillerIndex);
        Task<(int, int)> GetIndexes();
    }
}
