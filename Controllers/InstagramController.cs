using InstagramUploaderApi.Models;
using InstagramUploaderApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace InstagramUploaderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InstagramController : ControllerBase
    {
        private readonly InstagramService _instagramService;

        public InstagramController(
            InstagramService instagramService)
        {
            _instagramService = instagramService;
        }

        [HttpPost("publish")]
        public async Task<IActionResult> PublishPost(
            [FromBody] InstagramPublishRequest request)
        {
            if (string.IsNullOrEmpty(request.ImageUrl))
            {
                return BadRequest("ImageUrl is required");
            }

            var result =
                await _instagramService.PublishImageAsync(request);

            return Ok(result);
        }
    }
}