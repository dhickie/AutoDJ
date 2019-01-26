using System.Net.Http;
using System.Threading.Tasks;

namespace AutoDJ.Services
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> Send(HttpRequestMessage request);
    }
}
