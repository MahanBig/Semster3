using Heimevernet.Web.DataAccess;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("heimevernetdb"); // Tilkoblingsstrengen kommer fra Aspire (WithReference) eller fra user secrets.

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing connection string 'heimevernetdb'. Start the Aspire AppHost or " +
        "configure ConnectionStrings:heimevernetdb using user secrets.");
}

builder.Services.AddDbContext<HeimevernetDbContext>(options => // Registrerer databasen, slik at den kan brukes via dependency injection.
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddScoped<IResourceRepository, EfResourceRepository>();

var app = builder.Build();

// Apply table changes before adding sample records, following the reference project.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<HeimevernetDbContext>();
    dbContext.Database.Migrate(); 
    ResourceDbSeeder.Seed(dbContext);
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHttpsRedirection();
    app.UseHsts();
}
app.UseRouting(); // Rekkefølgen hver forespørsel går gjennom.

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
