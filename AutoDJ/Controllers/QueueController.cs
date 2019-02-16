using AutoDJ.Providers;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AutoDJ.Controllers
{
    [Route("api/queue")]
    [ApiController]
    public class QueueController : ControllerBase
    {
        private IQueueProvider _provider;

        public QueueController(IQueueProvider provider)
        {
            _provider = provider;
        }

        [HttpPut]
        [Route("{songKey}")]
        public async Task<IActionResult> AddSong(string songKey)
        {
            await _provider.AddSong(songKey);
            return Ok();
        }
    }
}
