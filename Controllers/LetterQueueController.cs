using Chefster.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Chefster.Controllers;

[Authorize]
[Route("api/letter-queue")]
[ApiController]
public class LetterQueueController(
    LetterQueueService letterQueueService,
    IConfiguration configuration
) : ControllerBase
{
    private readonly LetterQueueService _letterQueueService = letterQueueService;
    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    /// Populates a google sheet with customer information for sending personal letters
    /// </summary>
    [HttpPost]
    public ActionResult PopulateLetterQueue([FromBody] string apiKey)
    {
        if (apiKey.IsNullOrEmpty())
        {
            return BadRequest("Unauthorized!");
        }

        if (apiKey == _configuration["LETTER_QUEUE_KEY"])
        {
            _letterQueueService.PopulateLetterQueue();
            return Ok();
        }

        return BadRequest("Unauthorized!");
    }
}
