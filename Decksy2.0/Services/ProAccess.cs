using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Decksy2._0.Services
{
    // Pro features (vocabulary tests) are unlocked by the "Pro" role, which is granted by hand
    // with: dotnet Decksy2.0.dll grant-pro user@example.com
    public static class ProAccess
    {
        public const string Role = "Pro";

        // Checked against the database on every request, so a granted role works without logging in again.
        public static async Task<bool> IsProAsync(UserManager<IdentityUser> userManager, ClaimsPrincipal principal)
        {
            var user = await userManager.GetUserAsync(principal);
            return user != null && await userManager.IsInRoleAsync(user, Role);
        }

        public static async Task EnsureRoleAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roles.RoleExistsAsync(Role))
            {
                await roles.CreateAsync(new IdentityRole(Role));
            }
        }

        public static bool IsCommand(string[] args) =>
            args.Length > 0 && args[0] is "grant-pro" or "revoke-pro" or "list-pro";

        // Small admin CLI so Pro can be managed on the server without a database tool.
        public static async Task<int> RunCommandAsync(IServiceProvider services, string[] args)
        {
            using var scope = services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            if (args[0] == "list-pro")
            {
                var pros = await users.GetUsersInRoleAsync(Role);
                Console.WriteLine(pros.Count == 0 ? "No Pro users." : string.Join(Environment.NewLine, pros.Select(u => u.Email)));
                return 0;
            }

            if (args.Length < 2)
            {
                Console.WriteLine($"Usage: {args[0]} user@example.com");
                return 1;
            }

            var user = await users.FindByEmailAsync(args[1]);
            if (user == null)
            {
                Console.WriteLine($"No account with e-mail {args[1]}.");
                return 1;
            }

            var result = args[0] == "grant-pro"
                ? await users.IsInRoleAsync(user, Role) ? IdentityResult.Success : await users.AddToRoleAsync(user, Role)
                : await users.IsInRoleAsync(user, Role) ? await users.RemoveFromRoleAsync(user, Role) : IdentityResult.Success;

            if (!result.Succeeded)
            {
                Console.WriteLine(string.Join("; ", result.Errors.Select(e => e.Description)));
                return 1;
            }

            Console.WriteLine(args[0] == "grant-pro" ? $"{user.Email} now has Pro." : $"{user.Email} no longer has Pro.");
            return 0;
        }
    }
}
