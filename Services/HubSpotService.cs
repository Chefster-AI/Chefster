using System.Text;
namespace Chefster.Services;

public class HubSpotService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    private readonly HttpClient _client = httpClientFactory.CreateClient();

    public async void CreateContact(/* string ownerName, */ string emailAddress, /* UserStatus userStatus, */ string phoneNumber)
    {
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + configuration["HUBSPOT_API_KEY"]);

        var jsonBody = new {
            properties = new {
                // firstname = ownerName,
                email = emailAddress,
                // user_status = userStatus,
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
            // log here
        }
    }
}