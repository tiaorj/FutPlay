using Microsoft.AspNetCore.Identity;

namespace FutPlay.Services
{
    public static class IdentitySeedService
    {
        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            using var scope = services.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            await CriarRoleSeNecessarioAsync(roleManager, AppRoles.Administrador);
            await CriarRoleSeNecessarioAsync(roleManager, AppRoles.Participante);

            string? adminEmail = configuration["AdminSeed:Email"];
            string? adminPassword = configuration["AdminSeed:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                return;
            }

            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(admin, adminPassword);

                if (!createResult.Succeeded)
                {
                    var erros = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Não foi possível criar o usuário administrador inicial: {erros}");
                }
            }

            if (!await userManager.IsInRoleAsync(admin, AppRoles.Administrador))
            {
                var addRoleResult = await userManager.AddToRoleAsync(admin, AppRoles.Administrador);

                if (!addRoleResult.Succeeded)
                {
                    var erros = string.Join("; ", addRoleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Não foi possível associar o administrador inicial ao perfil Administrador: {erros}");
                }
            }
        }

        private static async Task CriarRoleSeNecessarioAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                return;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));

            if (!result.Succeeded)
            {
                var erros = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Não foi possível criar o perfil {roleName}: {erros}");
            }
        }
    }
}
