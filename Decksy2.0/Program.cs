using Decksy2._0.Data;
using Decksy2._0.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// "grant-pro <email>", "revoke-pro <email>" and "list-pro" run an admin command instead of the website.
var isAdminCommand = ProAccess.IsCommand(args);
var hostArgs = isAdminCommand ? Array.Empty<string>() : args;

var builder = WebApplication.CreateBuilder(hostArgs);

// Machine-specific settings (e.g. the connection string) live in appsettings.Local.json,
// which is git-ignored. Environment variables and command-line args still take precedence.
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(hostArgs);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found. Set it in appsettings.json, appsettings.Local.json or the ConnectionStrings__DefaultConnection environment variable.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Keep the keys that encrypt login cookies in a persistent folder (e.g. a Docker volume),
// otherwise every redeploy logs all users out.
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrEmpty(keysPath))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("Decksy");
}

// Behind a reverse proxy (Caddy/nginx) trust its X-Forwarded-* headers so the app
// knows the original request was HTTPS. The app itself is only reachable through the proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "DecksyAuth";       
    options.Cookie.HttpOnly = true;           
    options.Cookie.IsEssential = true;        
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;

    options.ExpireTimeSpan = TimeSpan.FromDays(30); 
    options.SlidingExpiration = false;            
});

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Create/upgrade the SQLite database file at startup (enabled in appsettings.json).
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
}

await ProAccess.EnsureRoleAsync(app.Services);

if (isAdminCommand)
{
    return await ProAccess.RunCommandAsync(app.Services, args);
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
return 0;
