using Amazon.SQS;
using Amazon.SQS.Model;
using Chefster.Common;
using Chefster.Models;
using Chefster.Services;
using Stripe;

namespace Chefster.Consumers;

public class StripeMessageConsumer(
    LoggingService loggingService,
    IServiceScopeFactory serviceScopeFactory,
    IConfiguration configuration
) : BackgroundService
{
    private readonly LoggingService _logger = loggingService;
    private readonly AmazonSQSClient _amazonSQSClient = new();
    private readonly IConfiguration _configuration = configuration;

    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

    public async Task MessageConsumer()
    {
        var queueUrl = _configuration["CALLBACK_QUEUE"]!;
        using var scope = _serviceScopeFactory.CreateScope();
        var _subscriberService = scope.ServiceProvider.GetRequiredService<SubscriberService>();
        while (true)
        {
            Console.WriteLine("Running bakground service");
            var receivedMessageRequest = new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 20
            };
            var response = await _amazonSQSClient.ReceiveMessageAsync(receivedMessageRequest);

            if (response.Messages.Count > 0)
            {
                foreach (var message in response.Messages)
                {
                    var stripeEvent = EventUtility.ParseEvent(message.Body);

                    try
                    {
                        switch (stripeEvent.Type)
                        {
                            //Successful Cases
                            case EventTypes.CheckoutSessionCompleted:
                                var sessionComplete =
                                    stripeEvent.Data.Object as Stripe.Checkout.Session;
                                var original = sessionComplete!.ClientReferenceId.Replace('_', '|');

                                var updated = new SubscriberUpdateDto
                                {
                                    CustomerId = sessionComplete!.CustomerId,
                                    SubscriptionId = sessionComplete!.SubscriptionId,
                                    PaymentCreatedDate = sessionComplete!.Created.ToString(),
                                    PaymentStatus =
                                        sessionComplete!.PaymentStatus == "paid"
                                            ? PaymentStatus.Paid
                                            : PaymentStatus.NotPaid,
                                };

                                var sessionCompleteUpdate =
                                    _subscriberService.UpdateSubscriberByFamilyId(
                                        original,
                                        updated
                                    );

                                if (sessionCompleteUpdate.Success)
                                {
                                    await DeleteMessage(message.ReceiptHandle);
                                }
                                break;

                            case EventTypes.ChargeSucceeded:
                                var charge = stripeEvent.Data.Object as Charge;

                                var chargeUpdated = new SubscriberUpdateDto
                                {
                                    ReceiptUrl = charge!.ReceiptUrl
                                };
                                var chargeUpdatedStatus =
                                    _subscriberService.UpdateSubscriberByCustomerId(
                                        charge!.CustomerId,
                                        chargeUpdated
                                    );

                                if (chargeUpdatedStatus!.Success)
                                {
                                    await DeleteMessage(message.ReceiptHandle);
                                }
                                break;

                            // Error Cases
                            case EventTypes.ChargeFailed:
                                var chargeFailed = stripeEvent.Data.Object as Charge;
                                var currentSub = _subscriberService.GetSubscriberByCustomerId(
                                    chargeFailed!.CustomerId
                                );

                                if (
                                    currentSub != null
                                    && currentSub.Data != null
                                    && currentSub.Data.PaymentCreatedDate != null
                                )
                                {
                                    var dateString = currentSub.Data.PaymentCreatedDate;
                                    DateTime parsedDate = DateTime.ParseExact(
                                        dateString!,
                                        "M/d/yyyy h:mm:ss tt",
                                        null
                                    );
                                    // skip updates that are old
                                    if (parsedDate < DateTime.UtcNow)
                                    {
                                        await DeleteMessage(message.ReceiptHandle);
                                        break;
                                    }
                                }
                                var chargeFailedUpdate = new SubscriberUpdateDto
                                {
                                    PaymentStatus =
                                        chargeFailed!.Status == "failed"
                                            ? PaymentStatus.NotPaid
                                            : PaymentStatus.Unknown
                                };
                                var chargeFailedUpdateStatus =
                                    _subscriberService.UpdateSubscriberByCustomerId(
                                        chargeFailed!.CustomerId,
                                        chargeFailedUpdate
                                    );

                                if (chargeFailedUpdateStatus!.Success)
                                {
                                    await DeleteMessage(message.ReceiptHandle);
                                }
                                break;
                            default:
                                _logger.Log(
                                    $"Received unhandled stripe callback {stripeEvent.Type}",
                                    LogLevels.Warning
                                );
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error processing message from callbackQueue: {e}");
                    }
                }
            }
        }
    }

    private async Task DeleteMessage(string receiptHandle)
    {
        await _amazonSQSClient.DeleteMessageAsync(_configuration["CALLBACK_QUEUE"], receiptHandle);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await MessageConsumer();
    }
}
