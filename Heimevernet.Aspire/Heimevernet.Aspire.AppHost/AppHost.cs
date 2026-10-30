using Heimevernet.Aspire.AppHost.MariaDb;

var builder = DistributedApplication.CreateBuilder(args);

var mariaDbServer = builder.AddMariaDb("mariadb") // Starter MariaDB som en Docker-container.
                   .WithDataBindMount(source: @"../../../MariaDb/Data")
                   .WithLifetime(ContainerLifetime.Persistent);

var mariaDb = mariaDbServer.AddDatabase("heimevernetdb");

// Innlogging for den første admin-brukeren. E-posten står i appsettings.json.
// Passordet er "secret": sett det med user secrets, ellers spør Aspire-dashboardet om det ved oppstart.
var adminEmail = builder.AddParameter("admin-email");
var adminPassword = builder.AddParameter("admin-password", secret: true);

builder.AddDockerfile("heimevernet-web", "../../", "Heimevernet.Web/Dockerfile") // Bygger web-appen fra Dockerfile og kjører den i Docker på port 8080.
                       .WithExternalHttpEndpoints()
                       .WithReference(mariaDb) 
                       .WaitFor(mariaDb)
                       .WithEnvironment("AdminUser__Email", adminEmail)       // "__" blir til ":" i .NET-konfigurasjon,
                       .WithEnvironment("AdminUser__Password", adminPassword) // altså AdminUser:Email og AdminUser:Password.
                       .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "heimevernet-web");


builder.Build().Run();
