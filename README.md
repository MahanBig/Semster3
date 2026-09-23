# Heimevernet

Heimevernet is a learning project built with ASP.NET Core MVC, .NET Aspire, and MariaDB. The solution currently contains the basic web application and the local development infrastructure that will support future features.

## What you need

Install these tools before starting:

- [.NET SDK 10.0.100](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- An editor or IDE, such as Visual Studio or Visual Studio Code

Docker Desktop must be running when you use Aspire to start MariaDB as a container. If you already have a MariaDB server, you can run the web project directly using its connection details instead.

You can check the .NET version with:

```powershell
dotnet --version
```

## Solution architecture

The solution is split into four projects:

| Project | Responsibility |
| --- | --- |
| `Heimevernet.Web` | The ASP.NET Core MVC website. It contains controllers, views, models, CSS, JavaScript, and static libraries. |
| `Heimevernet.Aspire.AppHost` | The local application orchestrator. It starts the website and MariaDB, connects them, and provides the Aspire dashboard. |
| `Heimevernet.Aspire.ServiceDefaults` | Shared service configuration for logging, health checks, service discovery, resilience, and OpenTelemetry. |
| `Heimevernet.Web.UnitTests` | Automated tests for the web project, using xUnit v3. |

The solution file is `Heimevernet.slnx`.

### How the parts connect

```text
Your browser
     |
     v
Heimevernet.Web (ASP.NET Core MVC)
     |
     | connection string supplied by Aspire
     v
MariaDB container

Heimevernet.Aspire.AppHost
     |
     +-- starts Heimevernet.Web
     +-- starts MariaDB
     +-- creates the heimevernetdb database
     +-- shows status in the Aspire dashboard
```

The AppHost is not the website itself. It is the program that describes the local application: which services exist, which containers should run, and which services depend on each other.

## How a request works

1. You open the website URL in a browser.
2. ASP.NET Core receives the request.
3. MVC routing selects a controller and action. For example, `/Home/Index` calls `HomeController.Index()`.
4. The controller returns a Razor view from `Heimevernet.Web/Views`.
5. The layout and static files provide the shared page structure and styling.

The MVC application has Home, Privacy, Error, and resource pages. Entity Framework Core connects to MariaDB through `HeimevernetDbContext`. On startup, the website applies the database migration that creates the `Resources` table and adds three sample records if that table is empty. The **Ressurser** menu opens `/ResourceHandler`, which lists the saved database records. Select a resource name to view its details.

## Start the complete application

Run these commands from the repository root:

```powershell
dotnet restore
dotnet run --project .\Heimevernet.Aspire\Heimevernet.Aspire.AppHost\Heimevernet.Aspire.AppHost.csproj
```

The AppHost will:

1. Start the MariaDB container.
2. Create the `heimevernetdb` database if it does not exist.
3. Start the ASP.NET Core web project.
4. Supply the database connection to the web project through Aspire service references.
5. Start the Aspire dashboard.

The terminal prints the local URLs for the web application and dashboard. Open the dashboard URL to see service status, logs, traces, and health checks.

Stop the application with `Ctrl+C`. MariaDB uses a persistent container lifetime, so the container can remain available between runs. Docker Desktop can be used to inspect or stop it.

## Run only the web project

You can run the MVC project without Aspire:

```powershell
dotnet run --project .\Heimevernet.Web\Heimevernet.Web.csproj
```

This does not start MariaDB or the Aspire dashboard. Following the reference project's startup pattern, the web project requires a connection string and a reachable MariaDB server at startup. Configure the connection using the user-secrets command below before starting the web project directly.

## MariaDB and Entity Framework Core

Entity Framework Core (EF Core) lets C# classes represent database records and translates queries and changes into SQL. The Pomelo provider is the package that enables EF Core to communicate with MariaDB.

The database setup follows the [`DataAccess` structure in UIA202_2026](https://github.com/espenlimi/UIA202_2026/tree/0e0a6ed71f24f5f4c46a62c8c67442571ac5a46c/Heimevernet.Web/DataAccess):

- `Heimevernet.Web/Models/Entities/Resource.cs` is the database entity: one C# object represents one row in the `Resources` table. It contains a generated `Id`, required `Name`, `Description`, and `Type` values, and optional address and telephone details.
- `Heimevernet.Web/DataAccess/HeimevernetDbContext.cs` defines the database context, which represents a session with the database. Its primary constructor (the parameters beside the class name) receives connection options and passes them to EF Core's `DbContext`. Its `Resources` property exposes the records as a `DbSet<Resource>`. `OnModelCreating` maps this entity to the `Resources` table and defines the maximum lengths and required fields, following the reference schema.
- `Heimevernet.Web/Migrations/20260922155705_CreateResourcesTable.cs` is the initial migration. A migration describes database structure changes; this one creates the `Resources` table. EF also records which migrations have been applied so that restarting the app does not recreate the table.
- `Heimevernet.Web/DataAccess/ResourceDbSeeder.cs` inserts the reference project's three sample resources when the table is empty. Seeding means adding initial data for development.
- `Heimevernet.Web/Program.cs` registers the context with dependency injection, then calls `Database.Migrate()` followed by `ResourceDbSeeder.Seed()` during startup. Applying migrations first ensures the table exists before inserting records. A controller or service can also request `HeimevernetDbContext` in its constructor, and ASP.NET Core supplies one instance per request and disposes it afterward.
- `Heimevernet.Web/DataAccess/HeimevernetDbContextFactory.cs` creates a context for EF command-line tools. This is called at design time, meaning while developing database changes, rather than while serving web requests.

When started through Aspire, the web project receives `ConnectionStrings:heimevernetdb` automatically from `.WithReference(mariaDb)`. No password needs to be added to `appsettings.json`.

At runtime, `ServerVersion.AutoDetect` connects to MariaDB to determine which SQL features the server supports. Because startup now requests the context, applies migrations, and seeds the table, the database must be reachable before the website can start. A missing connection string stops startup with an error explaining how to configure it.

For standalone development against your existing MariaDB server, choose the database you want this application to use and store its connection string in user secrets from the repository root. Replace the example values with your actual server address, port, database name, username, and password. The database user needs permission to apply the migration and read and insert resource records.

```powershell
dotnet user-secrets set "ConnectionStrings:heimevernetdb" "Server=YOUR_SERVER;Port=3306;Database=heimevernetdb;User ID=YOUR_USER;Password=YOUR_PASSWORD;" --project .\Heimevernet.Web\Heimevernet.Web.csproj
dotnet run --project .\Heimevernet.Web\Heimevernet.Web.csproj
```

Use `localhost` or `127.0.0.1` for a server on your own computer. User secrets are stored outside the repository so credentials are not committed to Git. They are a development convenience, not an encrypted password vault. Aspire may assign a different host port, so use the connection details for the running database if connecting to its container manually. Running the web project directly uses this configured server; running the AppHost supplies its own container connection.

The first successful startup against an empty `Resources` table adds:

| Name | Description | Type |
| --- | --- | --- |
| Resource 1 | Db Description 1 | Type A |
| Resource 2 | Db Description 2 | Type B |
| Resource 3 | Db Description 3 | Type C |

MariaDB generates each record's `Id`. If the table already contains any resource, the seeder skips all inserts. This preserves existing records and your edits across restarts. It does not replace individual sample records you delete while other records remain; if you empty the entire table, the next startup adds all three samples again.

You can inspect the saved data in your MariaDB client with:

```sql
SELECT Id, Name, Description, Type FROM Resources ORDER BY Id;
```

You can also see these records on the website:

1. Start the web project using either of the commands above.
2. Open the website address printed in the terminal or shown in Aspire, then select **Ressurser** (the `/ResourceHandler` page).
3. Select a resource name to open `/Resource/Index/{id}` and see its saved details. Refresh the page after changing data in MariaDB to read the latest values.

The reading flow follows the reference project's repository, controller, and view structure:

- `IResourceRepository` defines the two reading operations: `GetAll()` and `GetById(id)`. `EfResourceRepository` implements them using `HeimevernetDbContext.Resources`. `AsNoTracking()` tells EF that these queries display records without preparing to edit them.
- `Program.cs` registers the repository with `AddScoped`, so ASP.NET Core supplies a repository and its database context to each web request.
- `ResourceHandlerController.Index()` gets all records and maps their ID, name, description, and type into `ResourceViewModel` objects. The Razor view loops through that list to create the table, with a message if the list is empty.
- `ResourceController.Index(id)` reads the selected row for its details page. A missing ID redirects to the list, while an ID that does not exist returns HTTP 404 (not found).

This implements reading and displaying saved resources. Creating, editing, and deleting database records through the website remains a separate feature.

For deployment, the equivalent environment variable is `ConnectionStrings__heimevernetdb`.

The design-time factory follows the reference's separate configuration: it reads `ConnectionStrings__heimevernetdb` directly and uses MariaDB 10.11 as a SQL compatibility baseline. This fixed version lets EF generate migrations without connecting to MariaDB; it is not a claim about the running container's version. Without the environment variable, the factory uses a localhost placeholder with an empty password for offline generation. For EF commands that actually access the database, set the environment variable to a real connection string; the factory does not load the website's user secrets.

The project uses Pomelo 9.0.0 with EF Core 9.0.20. EF Core 9 works with the application's .NET 10 target; the provider must match EF Core's major version. See the [Pomelo compatibility table](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql#compatibility). The `Microsoft.EntityFrameworkCore.Design` package supports commands for generating and managing migrations. The included initial migration is applied automatically when the website starts successfully.

## Run the tests

Run all tests with:

```powershell
dotnet test --project .\Heimevernet.Web.UnitTests\Heimevernet.Web.UnitTests.csproj
```

The seeder tests use a temporary SQLite database to check that the three records are saved, repeated runs preserve edits without duplicates, and preexisting records prevent sample inserts. These tests do not verify connectivity to your MariaDB server.

Build the complete solution with:

```powershell
dotnet build .\Heimevernet.slnx
```

## Where to make changes
- Clone the project and open it in your IDE.
- Connect it to your own GitHub repository (optional).
- Add or update page actions in `Heimevernet.Web/Controllers`.
- Add page-specific data models in `Heimevernet.Web/Models`.
- Configure database access and add entity sets in `Heimevernet.Web/DataAccess/HeimevernetDbContext.cs`.
- Add Razor views in `Heimevernet.Web/Views`.
- Add styling in `Heimevernet.Web/wwwroot/css`.
- Add browser JavaScript in `Heimevernet.Web/wwwroot/js`.
- Configure local services in `Heimevernet.Aspire/Heimevernet.Aspire.AppHost/AppHost.cs`.
- Add database resource behavior in `Heimevernet.Aspire/Heimevernet.Aspire.AppHost/MariaDb`.
- Add automated tests in `Heimevernet.Web.UnitTests`.

## Useful beginner concepts

### ASP.NET Core MVC

MVC means Model-View-Controller:

- **Model** represents data.
- **View** generates the HTML shown in the browser.
- **Controller** receives requests and decides what response to return.

### .NET Aspire

Aspire helps run several related services together during development. Instead of starting the web application and database manually, the AppHost describes the whole local system in code.

### Dependency injection

Dependency injection is how ASP.NET Core supplies services to controllers and other classes. Services are registered during application startup and then requested where they are needed, rather than being created manually in every class.

### Automated tests

Tests execute code automatically and check expected behavior. A good test should make an assertion about a specific result; a test with no assertion can pass without proving that the application works.

## Development workflow

Work on a feature branch rather than directly on `main`:

```powershell
git switch -c {developer-name}/{feature-name}
```

Make small changes, run the relevant tests, and build the solution before sharing the branch.
