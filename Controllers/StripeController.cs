using System;
using System.Text;
using Amazon.SQS;
using Amazon.SQS.Model;
using Chefster.Common;
using Chefster.Models;
using Chefster.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Stripe;

namespace Chefster.Controllers;

[Route("api/stripe/callback")]
[ApiController]
public class StripeController(LoggingService loggingService, IConfiguration configuration)
    : ControllerBase
{
    private readonly LoggingService _logger = loggingService;
    private readonly AmazonSQSClient _amazonSQSClient = new();
    private readonly IConfiguration _configuration = configuration;

    public async Task<IActionResult> handleCallback()
    {
        var request = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var allowed = new HashSet<string>
        {
            EventTypes.InvoicePaymentSucceeded,
            EventTypes.CustomerSubscriptionCreated,
            EventTypes.CustomerSubscriptionUpdated,
            EventTypes.CheckoutSessionCompleted,
            EventTypes.ChargeFailed
        };

        try
        {
            var stripeEvent = EventUtility.ParseEvent(request);

            if (allowed.Contains(stripeEvent.Type))
            {
                await _amazonSQSClient.SendMessageAsync(
                    new SendMessageRequest
                    {
                        QueueUrl = _configuration["CALLBACK_QUEUE"],
                        MessageBody = request
                    }
                );
                Console.WriteLine($"[Queued] {stripeEvent.Type}");
            }
            return Ok();
        }
        catch (Exception e)
        {
            _logger.Log($"Error receiving callback: {e}", LogLevels.Error);
            return BadRequest();
        }
    }
}
