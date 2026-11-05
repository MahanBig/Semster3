using System.Globalization;
using Heimevernet.Web.DataAccess;
using Heimevernet.Web.DataAccess.Identity;
using Heimevernet.Web.Models.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Tilkoblingsstrengen kommer fra docker-compose.yml (miljøvariabelen ConnectionStrings__heimevernetdb),
// eller fra user secrets hvis du kjører web-prosjektet direkte i Visual Studio.
var connectionString = builder.Configuration.GetConnectionString("heimevernetdb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing connection string 'heimevernetdb'. Start the app with 'docker compose up --build' or " +
        "configure ConnectionStrings:heimevernetdb using user secrets.");
}

// Én felles "fabrikk" for databasetilkoblinger (singleton = samme objekt for hele appen).
// Repository og Identity-stores ber den om en ny tilkobling hver gang de skal kjøre SQL.
builder.Services.AddSingleton<IDbConnectionFactory>(new MySqlConnectionFactory(connectionString));
builder.Services.AddScoped<IResourceRepository, DapperResourceRepository>();
builder.Services.AddScoped<ICategoryRepository, DapperCategoryRepository>();
builder.Services.AddScoped<INeedRepository, DapperNeedRepository>();
builder.Services.AddScoped<IAllocationRepository, DapperAllocationRepository>();
builder.Services.AddScoped<IOrganizationRepository, DapperOrganizationRepository>();
builder.Services.AddScoped<IRoleVerificationRepository, DapperRoleVerificationRepository>();

// Identity: brukere (ApplicationUser) og roller (ApplicationRole).
// Identity tar seg av passord-hashing, cookies og regler. Lagringen gjør våre egne Dapper-klasser.
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.User.RequireUniqueEmail = true;      // To brukere kan ikke ha samme e-post.
        options.Password.RequiredLength = 8;         // Standard er 6.
        options.Lockout.MaxFailedAccessAttempts = 5; // Låser kontoen en stund etter 5 feil passord.
    })
    .AddUserStore<DapperUserStore>()  // Brukere -> AspNetUsers, AspNetUserRoles
    .AddRoleStore<DapperRoleStore>()  // Roller  -> AspNetRoles
    .AddDefaultTokenProviders();      // Trengs f.eks. for "glemt passord" senere.

// Innloggings-cookien krypteres med nøkler (Data Protection). I Docker lagres nøklene i et volum
// (DataProtection__KeysPath i docker-compose.yml), ellers ville alle blitt logget ut ved hver omstart.
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("Heimevernet");
}

// Hele appen krever innlogging. "FallbackPolicy" gjelder alle sider som ikke har en egen regel,
// så vi slipper å huske [Authorize] på hver controller. Den som ikke er logget inn, sendes til LoginPath.
// Sidene som MÅ være åpne (logg inn, registrer, feilside) har [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Innloggingen huskes i en cookie. Her sier vi hvor brukeren sendes når det trengs.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";               // Ikke logget inn, men siden krever det.
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied"; // Logget inn, men mangler riktig rolle.
});

var app = builder.Build();

// Lag tabellene (Schema.sql) før vi legger inn eksempeldata, rollene og admin-brukeren.
using (var scope = app.Services.CreateScope())
{
    var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    DatabaseInitializer.EnsureSchema(connectionFactory);
    ResourceDbSeeder.Seed(connectionFactory);

    // Lager rollene og en admin-bruker. E-post og passord kommer fra konfigurasjon, ikke fra koden.
    await IdentitySeeder.SeedAsync(
        scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(),
        scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
        scope.ServiceProvider.GetRequiredService<IRoleVerificationRepository>(),
        builder.Configuration["AdminUser:Email"],
        builder.Configuration["AdminUser:Password"],
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>());
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHttpsRedirection();
    app.UseHsts();
}

// Sikkerhets-headere på alle svar (krav 8). De forteller nettleseren hva den IKKE skal tillate:
//  - nosniff: ikke gjett filtype (hindrer at en tekstfil blir kjørt som script).
//  - DENY: siden kan ikke vises inne i en <iframe> på en annen side (hindrer "clickjacking").
//  - Referrer-Policy: send bare domenet, ikke hele adressen, når man klikker seg videre.
//  - Permissions-Policy: posisjon, kamera og mikrofon er av.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
    await next();
});
// Fast kultur for alle forespørsler: tall skrives og leses med punktum (58.1464), uansett språk på maskinen.
// Ellers ville koordinatene fra kartet bli lest feil på en norsk Windows-maskin, der desimaltegnet er komma.
// Datoene i viewene har eget format, så de påvirkes ikke.
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(CultureInfo.InvariantCulture),
    SupportedCultures = [CultureInfo.InvariantCulture],
    SupportedUICultures = [CultureInfo.InvariantCulture]
});

app.UseRouting(); // Rekkefølgen hver forespørsel går gjennom.

app.UseAuthentication(); // Leser innloggings-cookien og finner ut HVEM brukeren er.
app.UseAuthorization();  // Sjekker om brukeren har LOV til å se siden ([Authorize]).

app.MapStaticAssets().AllowAnonymous(); // CSS, JS og logo må lastes på innloggingssiden også.

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
