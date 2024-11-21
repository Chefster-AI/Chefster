using Amazon.SQS;
using Amazon.SQS.Model;
using Chefster.Common;
using Chefster.Models;
using Chefster.Services;
using Hangfire;
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
        var _addressService = scope.ServiceProvider.GetRequiredService<AddressService>();
        var _familyService = scope.ServiceProvider.GetRequiredService<FamilyService>();
        var _jobService = scope.ServiceProvider.GetRequiredService<JobService>();
        var _subscriptionService =
            scope.ServiceProvider.GetRequiredService<Services.SubscriptionService>();
        var _letterQueueService = scope.ServiceProvider.GetRequiredService<LetterQueueService>();

        while (true)
        {
            // Wait up to 20 seconds per interval for messages
            var receivedMessageRequest = new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 20
            };

            // Handle each message
            var response = await _amazonSQSClient.ReceiveMessageAsync(receivedMessageRequest);
            foreach (var message in response.Messages)
            {
                try
                {
                    var stripeEvent = EventUtility.ParseEvent(message.Body);
                    Console.WriteLine("[Handling]: " + stripeEvent.Type);
                    switch (stripeEvent.Type)
                    {
                        case EventTypes.InvoicePaymentSucceeded: // creates majority of subscription model
                            InvoicePaymentSucceeded(stripeEvent, message);
                            break;
                        case EventTypes.CustomerSubscriptionCreated:
                            UpdateUserStatus(stripeEvent, message);
                            break;
                        case EventTypes.CustomerSubscriptionUpdated: // updates UserStatus
                            UpdateUserStatus(stripeEvent, message);
                            break;
                        case EventTypes.CheckoutSessionCompleted: // contains user address
                            CreateAddress(stripeEvent, message);
                            break;
                        case EventTypes.ChargeFailed: // logs details for failed charges
                            ChargeFailed(stripeEvent, message);
                            break;
                        // TODO: handle unsubscription
                        default:
                            _logger.Log(
                                $"Received unhandled stripe callback {stripeEvent.Type}",
                                LogLevels.Error
                            );
                            break;
                    }
                }
                catch (Exception e)
                {
                    _logger.Log(
                        $"Error processing message from callbackQueue: {e}",
                        LogLevels.Error
                    );
                }

                await Task.Delay(2000);
            }
        }

        async void InvoicePaymentSucceeded(Event stripeEvent, Message message)
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            var exists = await _subscriptionService.GetSubscriptionById(invoice!.SubscriptionId);

            if (exists.Data != null)
            {
                _logger.Log(
                    $"Subscriptions already exists. {invoice!.SubscriptionId}",
                    LogLevels.Info
                );
                return;
            }
            var subscription = new SubscriptionModel
            {
                SubscriptionId = invoice!.SubscriptionId,
                CustomerId = invoice.CustomerId,
                Email = invoice.CustomerEmail,
                UserStatus = invoice.Total == 0 ? UserStatus.FreeTrial : UserStatus.Subscribed,
                InvoiceUrl = invoice.HostedInvoiceUrl,
                StartDate = invoice.Lines.Data[0].Period.Start,
                EndDate = invoice.Lines.Data[0].Period.End
            };

            await _subscriptionService.CreateSubscription(subscription);
            await DeleteMessage(message.ReceiptHandle);
        }

        async void UpdateUserStatus(Event stripeEvent, Message message)
        {
            var updatedSubscription = stripeEvent.Data.Object as Subscription;
            var response = await _subscriptionService.GetSubscriptionById(updatedSubscription!.Id);
            var newUserStatus = UserStatus.Unknown;

            var currentSubscription = response.Data;
            if (currentSubscription == null)
            {
                return;
            }

            // Determine new UserStatus based on the updated subscription's status
            switch (updatedSubscription.Status)
            {
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
                    newUserStatus =
                        currentSubscription.UserStatus == UserStatus.FreeTrial
                            ? UserStatus.FreeTrialEnded
                            : UserStatus.SubscriptionEnded;
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

            // Create recurring job if UserStatus moves to FreeTrial or Subscribed
            if (newUserStatus == UserStatus.FreeTrial || newUserStatus == UserStatus.Subscribed)
            {
                var family = _familyService.GetByEmail(currentSubscription.Email).Data;
                _jobService.CreateOrUpdateJob(family!.Id);
            }

            await _subscriptionService.UpdateUserStatus(
                currentSubscription.SubscriptionId,
                newUserStatus
            );
            await _familyService.UpdateUserStatusByEmail(currentSubscription.Email, newUserStatus);
            await DeleteMessage(message.ReceiptHandle);
        }

        async void CreateAddress(Event stripeEvent, Message message)
        {
            var checkoutSession = stripeEvent.Data.Object as Stripe.Checkout.Session;
            var familyId = checkoutSession!.ClientReferenceId.Replace('_', '|');
            var existingAddress = _addressService.GetAddress(familyId);

            // Create Address and add it to the LetterQueue if we don't have one for them yet
            if (existingAddress == null)
            {
                var address = new AddressModel
                {
                    FamilyId = familyId,
                    StreetAddress = checkoutSession.CustomerDetails.Address.Line1,
                    AptOrUnitNumber = checkoutSession.CustomerDetails.Address.Line2 ?? null,
                    CityOrTown = checkoutSession.CustomerDetails.Address.City,
                    StateProvinceRegion = checkoutSession.CustomerDetails.Address.State,
                    PostalCode = checkoutSession.CustomerDetails.Address.PostalCode,
                    Country = checkoutSession.CustomerDetails.Address.Country
                };

                await _addressService.CreateAddress(address);

                UserStatus status = checkoutSession!.AmountTotal == 0 ? UserStatus.FreeTrial : UserStatus.Subscribed;

                if (status == UserStatus.Subscribed)
                {
                    var family = _familyService.GetByEmail(checkoutSession.CustomerEmail).Data;
                    if (family != null)
                    {
                        var letterModel = new LetterModel
                        {
                            Email = checkoutSession.CustomerEmail,
                            Family = family,
                            Address = address
                        };
                        _letterQueueService.PopulateLetterQueue(letterModel);
                    }
                }
            }

            await DeleteMessage(message.ReceiptHandle);
        }

        async void ChargeFailed(Event stripeEvent, Message message)
        {
            var charge = stripeEvent.Data.Object as Charge;
            _logger.Log(
                $"Charge failed for Stripe customer {charge!.CustomerId}. Details: {charge.FailureMessage} {charge.Outcome.SellerMessage}",
                LogLevels.Warning
            );

            await DeleteMessage(message.ReceiptHandle);
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
