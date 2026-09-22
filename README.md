# Heimevernet

Heimevernet is a learning project built with ASP.NET Core MVC, .NET Aspire, and MariaDB. The solution currently contains the basic web application and the local development infrastructure that will support future features.

## What you need

Install these tools before starting:

- [.NET SDK 10.0.100](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- An editor or IDE, such as Visual Studio or Visual Studio Code

Docker Desktop must be running because Aspire starts MariaDB as a container.

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

The MVC application has Home, Privacy, Error, and resource pages. Entity Framework Core is connected to Aspire's MariaDB configuration through `HeimevernetDbContext`. The context does not yet contain database entities or migrations, and the resource pages still use their existing sample data.

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

This does not start MariaDB or the Aspire dashboard. Following the reference project's startup pattern, the web project requires a connection string even when run on its own. Configure it using the user-secrets command below before starting the web project directly.

## MariaDB and Entity Framework Core

Entity Framework Core (EF Core) lets C# classes represent database records and translates queries and changes into SQL. The Pomelo provider is the package that enables EF Core to communicate with MariaDB.

The database setup follows the [`DataAccess` structure in UIA202_2026](https://github.com/espenlimi/UIA202_2026/tree/0e0a6ed71f24f5f4c46a62c8c67442571ac5a46c/Heimevernet.Web/DataAccess):

- `Heimevernet.Web/DataAccess/HeimevernetDbContext.cs` defines the database context, which represents a session with the database. Its primary constructor (the parameters beside the class name) receives connection options and passes them to EF Core's `DbContext`. Future database entity classes will be exposed through `DbSet<TEntity>` properties here.
- `Heimevernet.Web/Program.cs` registers the context with dependency injection. A controller or service can request `HeimevernetDbContext` in its constructor, and ASP.NET Core supplies one instance per request and disposes it afterward.
- `Heimevernet.Web/DataAccess/HeimevernetDbContextFactory.cs` creates a context for EF command-line tools. This is called at design time, meaning while developing database changes, rather than while serving web requests.

When started through Aspire, the web project receives `ConnectionStrings:heimevernetdb` automatically from `.WithReference(mariaDb)`. No password needs to be added to `appsettings.json`.

At runtime, `ServerVersion.AutoDetect` connects to MariaDB when the context is requested to determine which SQL features the server supports. The database must be reachable at that point. A missing connection string stops website startup with an error explaining how to configure it.

For standalone development against an existing MariaDB database, store the connection string in user secrets from the repository root. Replace the example values with your actual database, port, username, and password:

```powershell
dotnet user-secrets set "ConnectionStrings:heimevernetdb" "Server=localhost;Port=3306;Database=heimevernetdb;User ID=YOUR_USER;Password=YOUR_PASSWORD;" --project .\Heimevernet.Web\Heimevernet.Web.csproj
dotnet run --project .\Heimevernet.Web\Heimevernet.Web.csproj
```

User secrets are stored outside the repository so credentials are not committed to Git. They are a development convenience, not an encrypted password vault. Aspire may assign a different host port, so use the connection details for the running database if connecting to its container manually.

For deployment, the equivalent environment variable is `ConnectionStrings__heimevernetdb`.

The design-time factory follows the reference's separate configuration: it reads `ConnectionStrings__heimevernetdb` directly and uses MariaDB 10.11 as a SQL compatibility baseline. This fixed version lets EF generate migrations without connecting to MariaDB; it is not a claim about the running container's version. Without the environment variable, the factory uses a localhost placeholder with an empty password for offline generation. For EF commands that actually access the database, set the environment variable to a real connection string; the factory does not load the website's user secrets.

The project uses Pomelo 9.0.0 with EF Core 9.0.20. EF Core 9 works with the application's .NET 10 target; the provider must match EF Core's major version. See the [Pomelo compatibility table](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql#compatibility). The `Microsoft.EntityFrameworkCore.Design` package supports future migration commands. A migration records changes to database tables; none are created or applied by this connection setup.

## Run the tests

Run all tests with:

```powershell
dotnet test --project .\Heimevernet.Web.UnitTests\Heimevernet.Web.UnitTests.csproj
```

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
