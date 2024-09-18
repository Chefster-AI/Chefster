using System.Text;
using Chefster.Common;
namespace Chefster.Services;

public class HubSpotService(IHttpClientFactory httpClientFactory, IConfiguration configuration, LoggingService loggingService)
{
    private readonly HttpClient _client = httpClientFactory.CreateClient();
    private readonly LoggingService _logger = loggingService;
    public async void CreateContact(string name, string emailAddress, UserStatus userStatus, string phoneNumber)
    {
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + configuration["HUBSPOT_API_KEY"]);

        var jsonBody = new {
            properties = new {
                firstname = name,
                email = emailAddress,
                user_status = userStatus.ToString(),
                phone = phoneNumber
            }
        };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(jsonBody),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("https://api.hubapi.com/crm/v3/objects/contacts", content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.Log($"Failed to create contact for email: {emailAddress}", LogLevels.Warning, "HubSpot Service Create Contact");
        }
    }

    public async void UpdateContact(string? name, string emailAddress, UserStatus? userStatus, string? phoneNumber)
    {
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + configuration["HUBSPOT_API_KEY"]);

        var properties = new Dictionary<string, object>
        {
            { "email", emailAddress }
        };

        if (!string.IsNullOrEmpty(name))
        {
            properties.Add("firstname", name);
        }

        if (userStatus.HasValue)
        {
            properties.Add("user_status", userStatus.ToString()!);
        }

        if (!string.IsNullOrEmpty(phoneNumber))
        {
            properties.Add("phone", phoneNumber);
        }

        var jsonBody = new
        {
            properties = properties
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(jsonBody),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PatchAsync($"https://api.hubapi.com/crm/v3/objects/contacts/{emailAddress}?idProperty=email", content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.Log($"Failed to update contact for email: {emailAddress}", LogLevels.Warning, "HubSpot Service Update Contact");
        }
    }
}