using AutoDJ.Providers;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AutoDJ.Controllers
{
    [Route("api/mode")]
    [ApiController]
    public class ModeController : ControllerBase
    {
        private readonly IModeProvider _provider;

        public ModeController(IModeProvider provider)
        {
            _provider = provider;
        }

        [HttpPost]
        [Route("{modeId}")]
        public async Task<IActionResult> SetMode(int modeId)
        {
            await _provider.SetMode(modeId);
            return Ok();
        }
    }
}
