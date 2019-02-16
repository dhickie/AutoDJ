using System.Threading.Tasks;

namespace AutoDJ.Providers
{
    public interface IQueueProvider
    {
        Task AddSong(string songKey);
    }
}
