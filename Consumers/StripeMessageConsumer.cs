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
        bool exists = false;
        while (true)
        {
            // Wait up to 20 seconds per interval for messages
            var receivedMessageRequest = new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 20
            };

            // act on each callback message
            var response = await _amazonSQSClient.ReceiveMessageAsync(receivedMessageRequest);
            foreach (var message in response.Messages)
            {
                try
                {
                    var stripeEvent = EventUtility.ParseEvent(message.Body);
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
                                UserStatus =
                                    sessionComplete!.PaymentStatus == "paid"
                                        ? UserStatus.Subscribed
                                        : UserStatus.NotPaid,
                            };

                            _logger.Log(
                                $"session complete payment status: {sessionComplete!.PaymentStatus}",
                                LogLevels.Info
                            );

                            var sessionCompleteUpdate =
                                _subscriberService.UpdateSubscriberByFamilyId(original, updated);

                            if (sessionCompleteUpdate.Success)
                            {
                                await DeleteMessage(message.ReceiptHandle);
                            }
                            break;

                        case EventTypes.ChargeSucceeded:
                            var charge = stripeEvent.Data.Object as Charge;
                            exists = _subscriberService.SubscriberExists(charge!.CustomerId).Data;
                            if (!exists)
                            {
                                _logger.Log(
                                    $"Subscriber doesn't exist for Id: {charge!.CustomerId}",
                                    LogLevels.Warning,
                                    "ChargeSucceeded Event"
                                );
                                break;
                            }

                            var chargeUpdated = new SubscriberUpdateDto
                            {
                                ReceiptUrl = charge!.ReceiptUrl
                            };
                            var chargeUpdatedStatus =
                                await _subscriberService.UpdateSubscriberByCustomerId(
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
                            exists = _subscriberService
                                .SubscriberExists(chargeFailed!.CustomerId)
                                .Data;

                            if (!exists)
                            {
                                _logger.Log(
                                    $"Subscriber doesn't exist for Id: {chargeFailed!.CustomerId}",
                                    LogLevels.Warning,
                                    "ChargeFailed Event"
                                );
                                break;
                            }

                            var currentSub = _subscriberService.GetSubscriberByCustomerId(
                                chargeFailed!.CustomerId
                            );

                            if (
                                currentSub.Data != null
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
                                    _logger.Log(
                                        "Deleting failed charge message because its older than a valid charge",
                                        LogLevels.Info
                                    );
                                    await DeleteMessage(message.ReceiptHandle);
                                    break;
                                }
                            }

                            var chargeFailedUpdate = new SubscriberUpdateDto
                            {
                                UserStatus =
                                    chargeFailed!.Status == "failed"
                                        ? UserStatus.NotPaid
                                        : UserStatus.Unknown
                            };
                            var chargeFailedUpdateStatus =
                                await _subscriberService.UpdateSubscriberByCustomerId(
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
                await Task.Delay(2000);
            }
        }
    }

    private async Task DeleteMessage(string receiptHandle)
    {
        await _amazonSQSClient.DeleteMessageAsync(_configuration["CALLBACK_QUEUE"], receiptHandle);
    }

    // Start the consumer
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await MessageConsumer();
    }
}
