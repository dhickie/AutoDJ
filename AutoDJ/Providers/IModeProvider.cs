using System.Threading.Tasks;

namespace AutoDJ.Providers
{
    public interface IModeProvider
    {
        Task SetMode(int modeId);
    }
}
