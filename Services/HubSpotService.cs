using System.Text;
using Chefster.Common;
using Microsoft.Net.Http.Headers;

namespace Chefster.Services;

public class HubSpotService(
    IConfiguration configuration,
    LoggingService loggingService
)
{
    private readonly LoggingService _logger = loggingService;

    public async void CreateContact(
        string name,
        string emailAddress,
        UserStatus userStatus,
        string phoneNumber
    )
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add(
            "Authorization",
            "Bearer " + configuration["HUBSPOT_API_KEY"]
        );

        var jsonBody = new
        {
            properties = new
            {
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

        var response = await client.PostAsync(
            "https://api.hubapi.com/crm/v3/objects/contacts",
            content
        );

        if (!response.IsSuccessStatusCode)
        {
            _logger.Log(
                $"Failed to create contact for email: {emailAddress}",
                LogLevels.Warning,
                "HubSpot Service Create Contact"
            );
        }
    }

    public async Task UpdateContact(
        string? name,
        string emailAddress,
        UserStatus? userStatus,
        string? phoneNumber
    )
    {
        var client = new HttpClient();
        var token = configuration["HUBSPOT_API_KEY"];
        client.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"Bearer {token}");

        var properties = new Dictionary<string, object> { { "email", emailAddress } };

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

        var jsonBody = new { properties = properties };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(jsonBody),
            Encoding.UTF8,
            "application/json"
        );

        var response = await client.PatchAsync(
            $"https://api.hubapi.com/crm/v3/objects/contacts/{emailAddress}?idProperty=email",
            content
        );

        if (!response.IsSuccessStatusCode)
        {
            _logger.Log(
                $"Failed to update contact for email: {emailAddress}",
                LogLevels.Warning,
                "HubSpot Service Update Contact"
            );
        }
    }
}
