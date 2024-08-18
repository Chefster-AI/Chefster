using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chefster.Controllers;

public class AccountController : Controller
{
    private static string GetProtocol()
    {
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development")
        {
            return "https";
        }
        return "http";
    }

    public async Task LogIn()
    {
        var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
            .WithParameter(
                "redirect_uri",
                $"{Url.Action("Index", "Index", null, GetProtocol())}callback"
            )
            .WithRedirectUri(Url.Action("Profile", "Index")!)
            .Build();

        await HttpContext.ChallengeAsync(
            Auth0Constants.AuthenticationScheme,
            authenticationProperties
        );
    }

    [Authorize]
    public async Task LogOut()
    {
        var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
            .WithRedirectUri(Url.Action("Index", "Index", null, GetProtocol())!)
            .Build();

        await HttpContext.SignOutAsync(
            Auth0Constants.AuthenticationScheme,
            authenticationProperties
        );
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public async Task SignUp()
    {
        var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
            .WithParameter("screen_hint", "signup")
            .WithParameter(
                "redirect_uri",
                $"{Url.Action("Index", "Index", null, GetProtocol())}callback"
            )
            .WithRedirectUri(Url.Action("Profile", "Index")!)
            .Build();

        await HttpContext.ChallengeAsync(
            Auth0Constants.AuthenticationScheme,
            authenticationProperties
        );
    }
}
