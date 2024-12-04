using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Chefster.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Net.Http.Headers;
using MongoDB.Bson;

namespace Chefster.Services;

public class AuthenticationService(
    LoggingService loggingService,
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor
)
{
    private readonly IConfiguration _configuration = configuration;
    private readonly LoggingService _logger = loggingService;
    private readonly IHttpContextAccessor _contextAccessor = httpContextAccessor;

    public async Task<bool> IsEmailVerifiedAsync()
    {
        var httpContext = _contextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.Log($"httpContext was null.", LogLevels.Error);
            return false;
        }
        var user = httpContext.User;
        var email = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        // avoid calls to auth0 if we dont need to
        var currentStatus = user.FindFirst("email_verified")?.Value;
        if (currentStatus != null && currentStatus == "true")
        {
            return true;
        }

        _logger.Log($"Getting Auth0 User Information for email: {email}", LogLevels.Info);

        var accessToken = await httpContext.GetTokenAsync("access_token");
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"Bearer {accessToken}");
        var domain = _configuration["AUTH_DOMAIN"];
        var request = await httpClient.GetAsync($"https://{domain}/userinfo");

        if (!request.IsSuccessStatusCode)
        {
            _logger.Log($"Failed to get user info for email: {email}", LogLevels.Error);
            return false;
        }

        var response = await request.Content.ReadAsStringAsync();
        bool emailVerified = false;
        try
        {
            emailVerified = JsonDocument
                .Parse(response)
                .RootElement.GetProperty("email_verified")
                .GetBoolean();
        }
        catch (Exception e)
        {
            _logger.Log(
                $"Failed to get email_verified value from object for email: {email}. Exception: {e}",
                LogLevels.Error
            );
            return false;
        }

        // refresh auth if email is verified
        if (emailVerified)
        {
            await RefreshAuth();
        }

        return emailVerified;
    }

    private async Task RefreshAuth()
    {
        var httpClient = new HttpClient();
        var domain = _configuration["AUTH_DOMAIN"];
        var clientId = _configuration["AUTH_CLIENT_ID"];
        var httpContext = _contextAccessor.HttpContext;

        if (httpContext == null)
        {
            _logger.Log($"httpContext was null.", LogLevels.Error);
            return;
        }
        var email = httpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        _logger.Log($"Refreshing Authentication for email: {email}", LogLevels.Info);

        var url = $"https://{domain}/oauth/token";
        var refreshToken = await httpContext!.GetTokenAsync("refresh_token");

        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.Log("No refresh token available for auth refresh", LogLevels.Warning);
            return;
        }

        var body = new
        {
            grant_type = "refresh_token",
            client_id = clientId,
            refresh_token = refreshToken,
            client_secret = _configuration["AUTH_CLIENT_SECRET"]
        };

        var strContent = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json"
        );

        // reauth the user
        var response = await httpClient.PostAsync(url, strContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.Log(
                $"Failed to refresh user auth. {email} {response.StatusCode} {response.Content.ToJson()}",
                LogLevels.Error
            );
            return;
        }

        string? token;
        try
        {
            token = JsonDocument
                .Parse(responseContent)
                .RootElement.GetProperty("id_token")
                .GetString();
        }
        catch (Exception e)
        {
            _logger.Log(
                $"error parsing id_token out of request for email: {email}. Exception: {e}",
                LogLevels.Error
            );
            return;
        }

        // grab new token and update the User Object
        var handler = new JwtSecurityTokenHandler();
        var newToken = handler.ReadJwtToken(token);

        var existingClaims = httpContext.User.Claims;

        // keep existing ones not present in the new token
        var mergedClaims = existingClaims
            .Where(existingClaim =>
                !newToken.Claims.Any(newClaim => newClaim.Type == existingClaim.Type)
            )
            .Concat(newToken.Claims)
            .ToList();

        mergedClaims.Add(new Claim("email_verified", "true"));
        var identity = new ClaimsIdentity(mergedClaims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // store it in a cookie for retrieval
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        httpContext.User = principal;
    }
}
