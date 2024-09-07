using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Chefster.Services;

public class LetterQueueService(FamilyService familyService, IConfiguration configuration)
{
    private readonly FamilyService _familyService = familyService;
    private readonly IConfiguration _configuration = configuration;
    private readonly string _sheetId = "1QSu9sJtQ6aKs_vHzMxPxwONpi8J6SkJ5bg3z08g1zoI";

    public void PopulateLetterQueue()
    {
        var families = _familyService.GatherFamiliesForLetterQueue();

        if (families.Data == null || families.Data.Count == 0 || !families.Success)
        {
            Console.WriteLine("Families list was either null or empty. Exiting...");
            return;
        }

        var service = CreateSpreadSheetService();
        var allFamilies = new List<List<object>>();
        foreach (var fam in families.Data)
        {
            string? fullAddress = null;
            string? AllMembers = null;
            var address = _familyService.GetAddressForFamily(fam!.Id).Data;
            var members = _familyService.GetMembers(fam!.Id).Data;

            // Console.WriteLine(address.ToJson());

            if (members != null && members.Count != 0)
            {
                AllMembers = string.Join(", ", members.Select(m => m.Name));
            }

            if (address != null)
            {
                fullAddress =
                    address.StreetAddress
                    + ", "
                    + address.AptOrUnitNumber
                    + " "
                    + address.CityOrTown
                    + ", "
                    + address.StateProvinceRegion
                    + " "
                    + address.PostalCode
                    + " "
                    + address.Country;
            }
            var letterFamily = new List<object>()
            {
                AllMembers ?? "No Members..",
                fam.SubscriptionStatus,
                fam.PhoneNumber,
                fam.Email,
                fam.FamilySize,
                fullAddress ?? "No Address.."
            };
            allFamilies.Add(letterFamily);
        }

        var row = 1;
        foreach (var fam in allFamilies)
        {
            var range = $"A{row + 1}:F{row + 1}";
            row += row;
            var valueRange = new ValueRange() { Values = [fam] };
            var appendRequest = service.Spreadsheets.Values.Update(valueRange, _sheetId, range);
            appendRequest.ValueInputOption = SpreadsheetsResource
                .ValuesResource
                .UpdateRequest
                .ValueInputOptionEnum
                .USERENTERED;
            var appendResult = appendRequest.Execute();
            Console.WriteLine(appendResult.UpdatedRows);
        }
    }

    private GoogleCredential AuthenticateWithGoogle()
    {
        try
        {
            var accountCreds = new JsonCredentialParameters
            {
                Type = "service_account",
                ProjectId = "chefster-434201",
                PrivateKeyId = _configuration["G_PRIV_KEY_ID"],
                PrivateKey = _configuration["G_PRIV_KEY"],
                ClientEmail = _configuration["G_CLIENT_EMAIL"],
                ClientId = _configuration["G_CLIENT_ID"],
                TokenUrl = _configuration["G_AUTH_URI"],
                TokenUri = _configuration["G_TOKEN_URI"],
                UniverseDomain = _configuration["G_UNI_DOMAIN"],
            };

            // Permission scope for the request
            string[] scopes = [SheetsService.Scope.Spreadsheets];
            return GoogleCredential.FromJsonParameters(accountCreds).CreateScoped(scopes);
        }
        catch (Google.GoogleApiException e)
        {
            throw new Exception($"Exception Creating credentials: {e}");
        }
    }

    private SheetsService CreateSpreadSheetService()
    {
        try
        {
            var service = new SheetsService(
                new Google.Apis.Services.BaseClientService.Initializer()
                {
                    HttpClientInitializer = AuthenticateWithGoogle(),
                    ApplicationName = "Chefster",
                }
            );
            return service;
        }
        catch (Google.GoogleApiException e)
        {
            throw new Exception($"Exception Creating service: {e}");
        }
    }
}
