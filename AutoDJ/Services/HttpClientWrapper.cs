using System.Net.Http;
using System.Threading.Tasks;

namespace AutoDJ.Services
{
    public class HttpClientWrapper : IHttpClient
    {
        private readonly HttpClient _client;

        public HttpClientWrapper()
        {
            _client = new HttpClient();
        }

        public Task<HttpResponseMessage> Send(HttpRequestMessage request)
        {
            return _client.SendAsync(request);
        }
    }
}