using Heimevernet.Web.DataAccess;
using Heimevernet.Web.DataAccess.Identity;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("heimevernetdb"); // Tilkoblingsstrengen kommer fra Aspire (WithReference) eller fra user secrets.

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing connection string 'heimevernetdb'. Start the Aspire AppHost or " +
        "configure ConnectionStrings:heimevernetdb using user secrets.");
}

// Én felles "fabrikk" for databasetilkoblinger (singleton = samme objekt for hele appen).
// Repository og Identity-stores ber den om en ny tilkobling hver gang de skal kjøre SQL.
builder.Services.AddSingleton<IDbConnectionFactory>(new MySqlConnectionFactory(connectionString));
builder.Services.AddScoped<IResourceRepository, DapperResourceRepository>();

// Identity: brukere (IdentityUser) og roller (IdentityRole).
// Identity tar seg av passord-hashing, cookies og regler. Lagringen gjør våre egne Dapper-klasser.
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;      // To brukere kan ikke ha samme e-post.
        options.Password.RequiredLength = 8;         // Standard er 6.
        options.Lockout.MaxFailedAccessAttempts = 5; // Låser kontoen en stund etter 5 feil passord.
    })
    .AddUserStore<DapperUserStore>()  // Brukere -> AspNetUsers, AspNetUserRoles
    .AddRoleStore<DapperRoleStore>()  // Roller  -> AspNetRoles
    .AddDefaultTokenProviders();      // Trengs f.eks. for "glemt passord" senere.

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
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(),
        scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>(),
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
app.UseRouting(); // Rekkefølgen hver forespørsel går gjennom.

app.UseAuthentication(); // Leser innloggings-cookien og finner ut HVEM brukeren er.
app.UseAuthorization();  // Sjekker om brukeren har LOV til å se siden ([Authorize]).

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
