using Chefster.Common;
using Chefster.Models;
using Chefster.ViewModels;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Chefster.Services;

public class LetterQueueService(IConfiguration configuration, LoggingService loggingService)
{
    private readonly IConfiguration _configuration = configuration;
    private readonly LoggingService _logger = loggingService;
    private readonly string _sheetId = "1QSu9sJtQ6aKs_vHzMxPxwONpi8J6SkJ5bg3z08g1zoI";

    public void PopulateLetterQueue(
        FamilyViewModel family,
        string familyId,
        UserStatus userStatus,
        string email
    )
    {
        if (family == null)
        {
            _logger.Log($"family was null", LogLevels.Error);
            return;
        }

        var service = CreateSpreadSheetService();

        // make a list of members.
        var allMembers = "";
        if (family.Members != null && family.Members.Count != 0)
        {
            allMembers = string.Join(", ", family.Members.Select(m => m.Name));
        }

        // Construct address in correct form
        string? fullAddress = null;
        if (family.Address != null)
        {
            fullAddress =
                family.Address.StreetAddress
                + ", "
                + family.Address.AptOrUnitNumber
                + " "
                + family.Address.CityOrTown
                + ", "
                + family.Address.StateProvinceRegion
                + " "
                + family.Address.PostalCode
                + " "
                + family.Address.Country;
        }

        var letterFamily = new List<object>()
        {
            family.Name,
            allMembers,
            userStatus.ToString(),
            family.PhoneNumber,
            email,
            family.FamilySize,
            fullAddress ?? "No Address"
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
                $"Request to update sheet failed, appendResult was null or number of updated rows was 0. FamilyId: {familyId}",
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
