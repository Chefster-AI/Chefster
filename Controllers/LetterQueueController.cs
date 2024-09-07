using Chefster.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chefster.Controllers;

#if DEBUG
[Authorize]
[Route("api/letter-queue")]
[ApiController]
public class LetterQueueController(LetterQueueService letterQueueService) : ControllerBase
{
    private readonly LetterQueueService _letterQueueService = letterQueueService;

    /// <summary>
    /// Populates a google sheet with customer information for sending personal letters
    /// </summary>
    [HttpPost]
    public void PopulateLetterQueue()
    {
        _letterQueueService.PopulateLetterQueue();
    }
}

#endif
