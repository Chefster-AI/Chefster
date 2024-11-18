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
        var _familyService = scope.ServiceProvider.GetRequiredService<FamilyService>();
        var _addressService = scope.ServiceProvider.GetRequiredService<AddressService>();
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
                    Console.WriteLine("[Handling]: " + stripeEvent.Type);
                    switch (stripeEvent.Type)
                    {
                        case EventTypes.InvoicePaymentSucceeded: // creates majority of subscriber model
                            InvoicePaymentSucceeded(stripeEvent, message);
                            break;
                        case EventTypes.CustomerSubscriptionCreated: // updates UserStatus
                            UpdateUserStatus(stripeEvent, message);
                            break;
                        case EventTypes.CustomerSubscriptionUpdated: // updates UserStatus
                            UpdateUserStatus(stripeEvent, message);
                            break;
                        case EventTypes.CheckoutSessionCompleted: // contains user address, email, and id
                            CreateAddress(stripeEvent, message);
                            break;
                        // case EventTypes.ChargeSucceeded:
                        //     ChargeSucceeded(stripeEvent, message);
                        //     break;
                        // case EventTypes.ChargeFailed:
                        //     ChargeFailed(stripeEvent, message);
                        //     break;
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

        async void InvoicePaymentSucceeded(Event stripeEvent, Message message)
        {
            var invoice = stripeEvent.Data.Object as Invoice;

            var subscriber = new SubscriberModel
            {
                SubscriptionId = invoice!.SubscriptionId,
                CustomerId = invoice.CustomerId,
                Email = invoice.CustomerEmail,
                UserStatus = UserStatus.Paid,
                InvoiceUrl = invoice.HostedInvoiceUrl,
                StartDate = invoice.Lines.Data[0].Period.Start,
                EndDate = invoice.Lines.Data[0].Period.End
            };

            await _subscriberService.CreateSubscriber(subscriber);
            await DeleteMessage(message.ReceiptHandle);
        }

        async void UpdateUserStatus(Event stripeEvent, Message message)
        {
            var subscription = stripeEvent.Data.Object as Subscription;
            var response = await _subscriberService.GetSubscriberById(subscription!.Id);
            var subscriber = response.Data;
            if (subscriber == null) { return; }
            var newUserStatus = UserStatus.Unknown;

            // Determine new UserStatus based on the Subscription status
            switch (subscription.Status) {
                case "trialing":
                    newUserStatus = UserStatus.FreeTrial;
                    break;
                case "paused":
                    newUserStatus = UserStatus.FreeTrialEnded;
                    break;
                case "active":
                    newUserStatus = UserStatus.Subscribed;
                    break;
                case "canceled":
                    newUserStatus = subscriber.UserStatus == UserStatus.FreeTrial ? UserStatus.FreeTrialEnded : UserStatus.SubscriptionEnded;
                    break;
                case "unpaid":
                    newUserStatus = UserStatus.NotPaid;
                    break;
                case "past_due":
                    newUserStatus = UserStatus.NotPaid;
                    break;
                default:
                    newUserStatus = UserStatus.Unknown;
                    break;
            }
            
            await _subscriberService.UpdateUserStatus(subscriber.SubscriptionId, newUserStatus);
            await _familyService.UpdateUserStatusByEmail(subscriber.Email, newUserStatus);
            await DeleteMessage(message.ReceiptHandle);
        }

        async void CreateAddress(Event stripeEvent, Message message)
        {
            var checkoutSession = stripeEvent.Data.Object as Stripe.Checkout.Session;
            var address = new AddressModel
            {
                FamilyId = checkoutSession!.ClientReferenceId.Replace('_', '|'),
                StreetAddress = checkoutSession.CustomerDetails.Address.Line1,
                AptOrUnitNumber = checkoutSession.CustomerDetails.Address.Line2 ?? null,
                CityOrTown = checkoutSession.CustomerDetails.Address.City,
                StateProvinceRegion = checkoutSession.CustomerDetails.Address.State,
                PostalCode = checkoutSession.CustomerDetails.Address.PostalCode,
                Country = checkoutSession.CustomerDetails.Address.Country
            };

            await _addressService.CreateAddress(address);
            await DeleteMessage(message.ReceiptHandle);
        }

        /*
        async void CheckoutSessionCompleted(Event stripeEvent, Message message)
        {
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
        }

        async void ChargeSucceeded(Event stripeEvent, Message message)
        {
            var charge = stripeEvent.Data.Object as Charge;
            exists = _subscriberService.SubscriberExists(charge!.CustomerId).Data;
            if (!exists)
            {
                _logger.Log(
                    $"Subscriber doesn't exist for Id: {charge!.CustomerId}",
                    LogLevels.Warning,
                    "ChargeSucceeded Event"
                );
                return;
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
        }

        async void ChargeFailed(Event stripeEvent, Message message)
        {
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
                return;
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
        }
        */
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
