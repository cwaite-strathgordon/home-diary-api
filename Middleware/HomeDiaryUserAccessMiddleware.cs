using HomeDiary_api.Repositories;
using HomeDiary_api.Security;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeDiary_api.Middleware;

public class HomeDiaryUserAccessMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUserRepository users, ClientContext clientContext)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            !context.Request.Path.Equals("/api/auth/upsert-oauth-user", StringComparison.OrdinalIgnoreCase))
        {
            var oauthId = context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = string.IsNullOrWhiteSpace(oauthId)
                ? null
                : await users.GetByOAuthAsync("auth0", oauthId);

            if (user is null || user.Disabled)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Title = user?.Disabled == true ? "Account disabled" : "HomeDiary account not found",
                    Detail = user?.Disabled == true
                        ? "Your HomeDiary account has been disabled."
                        : "Complete the HomeDiary sign-in process before accessing the API."
                });
                return;
            }

            if (context.User.Identity is ClaimsIdentity identity)
            {
                identity.AddClaim(new Claim("homediary_user_id", user.UserId.ToString()));
                identity.AddClaim(new Claim("homediary_admin", user.Admin ? "true" : "false"));
                if (user.ClientId is int clientId)
                {
                    identity.AddClaim(new Claim("homediary_client_id", clientId.ToString()));
                    clientContext.Set(clientId, user.UserId);
                }
            }

            if (user.ClientId is null && !IsClientlessEndpoint(context.Request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Title = "Client onboarding required",
                    Detail = "Complete the property setup wizard before accessing HomeDiary."
                });
                return;
            }
        }

        await next(context);
    }

    private static bool IsClientlessEndpoint(PathString path) =>
        path.StartsWithSegments("/api/onboarding", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase);
}
