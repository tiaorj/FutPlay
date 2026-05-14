using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace FutPlay.Services
{
    public class EnsureParticipantRoleMiddleware
    {
        private readonly RequestDelegate _next;

        public EnsureParticipantRoleMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<IdentityUser> userManager)
        {
            if (context.User.Identity?.IsAuthenticated == true &&
                !context.User.IsInRole(AppRoles.Administrador) &&
                !context.User.IsInRole(AppRoles.Participante))
            {
                var user = await userManager.GetUserAsync(context.User);

                if (user != null)
                {
                    var roles = await userManager.GetRolesAsync(user);

                    if (!roles.Contains(AppRoles.Administrador) &&
                        !roles.Contains(AppRoles.Participante))
                    {
                        await userManager.AddToRoleAsync(user, AppRoles.Participante);
                    }

                    if (context.User.Identity is ClaimsIdentity identity &&
                        !identity.HasClaim(identity.RoleClaimType, AppRoles.Participante))
                    {
                        identity.AddClaim(new Claim(identity.RoleClaimType, AppRoles.Participante));
                    }
                }
            }

            await _next(context);
        }
    }
}
