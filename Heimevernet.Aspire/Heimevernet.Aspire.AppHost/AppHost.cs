using Heimevernet.Aspire.AppHost.MariaDb;

var builder = DistributedApplication.CreateBuilder(args);

var mariaDbServer = builder.AddMariaDb("mariadb") // Starter MariaDB som en Docker-container.
                   .WithDataBindMount(source: @"../../../MariaDb/Data")
                   .WithLifetime(ContainerLifetime.Persistent);

var mariaDb = mariaDbServer.AddDatabase("heimevernetdb");

builder.AddDockerfile("heimevernet-web", "../../", "Heimevernet.Web/Dockerfile") // Bygger web-appen fra Dockerfile og kjører den i Docker på port 8080.
                       .WithExternalHttpEndpoints()
                       .WithReference(mariaDb) 
                       .WaitFor(mariaDb)
                       .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "heimevernet-web");


builder.Build().Run();
