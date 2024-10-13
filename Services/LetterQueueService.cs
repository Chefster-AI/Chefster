using Chefster.Common;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Chefster.Services;

public class LetterQueueService(
    FamilyService familyService,
    IConfiguration configuration,
    LoggingService loggingService
)
{
    private readonly FamilyService _familyService = familyService;
    private readonly IConfiguration _configuration = configuration;
    private readonly LoggingService _logger = loggingService;
    private readonly string _sheetId = "1QSu9sJtQ6aKs_vHzMxPxwONpi8J6SkJ5bg3z08g1zoI";

    public void PopulateLetterQueue(string familyId)
    {
        var family = _familyService.GetById(familyId).Data;
        var familyAddress = _familyService.GetAddressForFamily(familyId).Data;
        var members = _familyService.GetMembers(familyId).Data;

        if (family == null)
        {
            _logger.Log(
                $"Familed to get family with Id: {familyId}. Exiting...",
                LogLevels.Warning,
                "PopulateLetterQueue"
            );
            return;
        }

        var service = CreateSpreadSheetService();

        // make a list of members.
        var allMembers = "";
        if (members != null && members.Count != 0)
        {
            allMembers = string.Join(", ", members.Select(m => m.Name));
        }

        // Construct address in correct form
        var fullAddress = "";
        if (familyAddress != null)
        {
            fullAddress =
                familyAddress.StreetAddress
                + ", "
                + familyAddress.AptOrUnitNumber
                + " "
                + familyAddress.CityOrTown
                + ", "
                + familyAddress.StateProvinceRegion
                + " "
                + familyAddress.PostalCode
                + " "
                + familyAddress.Country;
        }

        if (string.IsNullOrEmpty(fullAddress) || string.IsNullOrEmpty(allMembers))
        {
            _logger.Log(
                $"Attempted to add family with Id: {family.Id} to letter Queue without address or members",
                LogLevels.Error,
                "PopulateLetterQueue"
            );
            return;
        }

        var letterFamily = new List<object>()
        {
            family.Name,
            allMembers,
            family.UserStatus,
            family.PhoneNumber,
            family.Email,
            family.FamilySize,
            fullAddress
        };

        var valueRange = new ValueRange() { Values = [letterFamily] };
        var appendRequest = service.Spreadsheets.Values.Append(valueRange, _sheetId, "Sheet1!A1");
        appendRequest.ValueInputOption = SpreadsheetsResource
            .ValuesResource
            .AppendRequest
            .ValueInputOptionEnum
            .USERENTERED;

        var appendResult = appendRequest.Execute();
        if (appendResult == null || appendResult.Updates.UpdatedRows == 0)
        {
            _logger.Log(
                $"Request to update sheet failed, appendResult was null or number of updated rows was 0. FamilyId: {family.Id}",
                LogLevels.Error
            );
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
            _logger.Log($"Exception Creating google sheets credentials: {e}", LogLevels.Error);
            throw new Exception($"Exception Creating google sheets credentials: {e}");
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
            _logger.Log($"Exception Creating spreadsheet service: {e}", LogLevels.Error);
            throw new Exception($"Exception Creating spreadsheet service: {e}");
        }
    }
}
