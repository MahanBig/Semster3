-- Kjøres ved oppstart. IF NOT EXISTS endrer ikke tabeller som finnes, så kjør docker compose down -v etter endringer.
-- Tabellene som starter med AspNet, er tabellene til Identity (brukere og roller). Resten kommer fra ER-diagrammet.
-- Testene har en kopi av tabellene i SQLite-syntaks (SqliteTestDatabase.cs). Endrer du her, må du endre der også.

CREATE TABLE IF NOT EXISTS AspNetUsers (
    Id VARCHAR(255) NOT NULL,
    UserName VARCHAR(256) NULL,
    NormalizedUserName VARCHAR(256) NULL,
    Email VARCHAR(256) NULL,
    NormalizedEmail VARCHAR(256) NULL,
    EmailConfirmed BOOLEAN NOT NULL,
    PasswordHash LONGTEXT NULL,
    SecurityStamp LONGTEXT NULL,
    ConcurrencyStamp LONGTEXT NULL,
    PhoneNumber VARCHAR(20) NULL,
    PhoneNumberConfirmed BOOLEAN NOT NULL,
    TwoFactorEnabled BOOLEAN NOT NULL,
    LockoutEnd DATETIME(6) NULL,
    LockoutEnabled BOOLEAN NOT NULL,
    AccessFailedCount INT NOT NULL,
    Name VARCHAR(100) NULL,
    Consent BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (Id),
    UNIQUE INDEX UserNameIndex (NormalizedUserName),
    INDEX EmailIndex (NormalizedEmail)
);

CREATE TABLE IF NOT EXISTS AspNetRoles (
    Id VARCHAR(255) NOT NULL,
    Name VARCHAR(256) NULL,
    NormalizedName VARCHAR(256) NULL,
    ConcurrencyStamp LONGTEXT NULL,
    Description VARCHAR(255) NULL,
    AccessLevel TINYINT NOT NULL DEFAULT 0,
    RequiresVerification BOOLEAN NOT NULL DEFAULT FALSE,
    PRIMARY KEY (Id),
    UNIQUE INDEX RoleNameIndex (NormalizedName)
);

-- En rolle som krever godkjenning teller ikke før VerifiedAt har en verdi.
CREATE TABLE IF NOT EXISTS AspNetUserRoles (
    UserId VARCHAR(255) NOT NULL,
    RoleId VARCHAR(255) NOT NULL,
    VerifiedBy VARCHAR(255) NULL,
    VerifiedAt DATETIME NULL,
    PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES AspNetRoles (Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_AspNetUsers_VerifiedBy FOREIGN KEY (VerifiedBy) REFERENCES AspNetUsers (Id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS PostalCodes (
    PostalCode CHAR(4) NOT NULL,
    PostalPlace VARCHAR(50) NOT NULL,
    PRIMARY KEY (PostalCode)
);

-- Latitude, Longitude og Description brukes ikke i koden ennå.
CREATE TABLE IF NOT EXISTS Locations (
    Id INT NOT NULL AUTO_INCREMENT,
    Address VARCHAR(150) NULL,
    PostalCode CHAR(4) NULL,
    Latitude DECIMAL(9,6) NULL,
    Longitude DECIMAL(9,6) NULL,
    Description VARCHAR(255) NULL,
    PRIMARY KEY (Id),
    CONSTRAINT FK_Locations_PostalCodes FOREIGN KEY (PostalCode) REFERENCES PostalCodes (PostalCode)
);

CREATE TABLE IF NOT EXISTS Categories (
    Id INT NOT NULL AUTO_INCREMENT,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(255) NULL,
    PRIMARY KEY (Id),
    UNIQUE INDEX CategoryNameIndex (Name)
);

CREATE TABLE IF NOT EXISTS Organizations (
    Id INT NOT NULL AUTO_INCREMENT,
    Name VARCHAR(100) NOT NULL,
    Type VARCHAR(50) NULL,
    PRIMARY KEY (Id)
);

-- Kompetanser og koblingstabellene UserOrganizations, UserCompetences og ResourceCompetences
-- er med fra ER-diagrammet, men brukes ikke i koden ennå.
CREATE TABLE IF NOT EXISTS Competences (
    Id INT NOT NULL AUTO_INCREMENT,
    Name VARCHAR(100) NOT NULL,
    Certification VARCHAR(100) NULL,
    Description VARCHAR(255) NULL,
    PRIMARY KEY (Id)
);

-- ENUM-verdiene i Resources og Needs må være helt like konstantene i Models/StatusValues.cs.
-- ON DELETE SET NULL: slettes en bruker, blir ressursene og behovene hen la inn stående uten eier.
CREATE TABLE IF NOT EXISTS Resources (
    Id INT NOT NULL AUTO_INCREMENT,
    Name VARCHAR(100) NOT NULL,
    Description TEXT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    Status ENUM('tilgjengelig', 'i_bruk', 'utilgjengelig') NOT NULL DEFAULT 'tilgjengelig',
    RegisteredAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UserId VARCHAR(255) NULL,
    CategoryId INT NOT NULL,
    LocationId INT NULL,
    ContactName VARCHAR(100) NULL,  -- Kontaktinfo for akkurat denne ressursen (valgfritt).
    ContactPhone VARCHAR(20) NULL,  -- Tom = eierens telefon fra AspNetUsers vises i stedet.
    ContactEmail VARCHAR(255) NULL,
    PRIMARY KEY (Id),
    CONSTRAINT FK_Resources_AspNetUsers FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE SET NULL,
    CONSTRAINT FK_Resources_Categories FOREIGN KEY (CategoryId) REFERENCES Categories (Id),
    CONSTRAINT FK_Resources_Locations FOREIGN KEY (LocationId) REFERENCES Locations (Id) ON DELETE SET NULL
);

-- Kontaktfeltene kom etter at noen allerede hadde laget databasen. ADD COLUMN IF NOT EXISTS (MariaDB)
-- legger dem til i en eksisterende tabell, så ingen trenger å slette dataene sine.
ALTER TABLE Resources
    ADD COLUMN IF NOT EXISTS ContactName VARCHAR(100) NULL AFTER LocationId,
    ADD COLUMN IF NOT EXISTS ContactPhone VARCHAR(20) NULL AFTER ContactName,
    ADD COLUMN IF NOT EXISTS ContactEmail VARCHAR(255) NULL AFTER ContactPhone;

-- Bildet til en ressurs. Egen tabell (1:0..1 mot Resources) fordi bildedataene er store:
-- da hentes de bare når bildet faktisk skal vises, ikke hver gang listen med ressurser leses.
-- ResourceId er både primærnøkkel og fremmednøkkel, så en ressurs kan ha høyst ett bilde.
-- ContentType settes av serveren ut fra filens innhold (JPEG, PNG eller WebP), ikke det nettleseren påstår.
CREATE TABLE IF NOT EXISTS ResourceImages (
    ResourceId INT NOT NULL,
    ContentType VARCHAR(50) NOT NULL,
    Data MEDIUMBLOB NOT NULL,
    UploadedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (ResourceId),
    CONSTRAINT FK_ResourceImages_Resources FOREIGN KEY (ResourceId) REFERENCES Resources (Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Needs (
    Id INT NOT NULL AUTO_INCREMENT,
    Title VARCHAR(100) NOT NULL,
    Description TEXT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    Priority ENUM('lav', 'middels', 'høy', 'kritisk') NOT NULL DEFAULT 'middels',
    Status ENUM('åpen', 'under_behandling', 'dekket') NOT NULL DEFAULT 'åpen',
    RegisteredAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Deadline DATETIME NULL,
    UserId VARCHAR(255) NULL,
    OrganizationId INT NULL,
    CategoryId INT NOT NULL,
    LocationId INT NULL,
    PRIMARY KEY (Id),
    CONSTRAINT FK_Needs_AspNetUsers FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE SET NULL,
    CONSTRAINT FK_Needs_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (Id) ON DELETE SET NULL,
    CONSTRAINT FK_Needs_Categories FOREIGN KEY (CategoryId) REFERENCES Categories (Id),
    CONSTRAINT FK_Needs_Locations FOREIGN KEY (LocationId) REFERENCES Locations (Id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS UserOrganizations (
    UserId VARCHAR(255) NOT NULL,
    OrganizationId INT NOT NULL,
    PRIMARY KEY (UserId, OrganizationId),
    CONSTRAINT FK_UserOrganizations_AspNetUsers FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserOrganizations_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS UserCompetences (
    UserId VARCHAR(255) NOT NULL,
    CompetenceId INT NOT NULL,
    PRIMARY KEY (UserId, CompetenceId),
    CONSTRAINT FK_UserCompetences_AspNetUsers FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserCompetences_Competences FOREIGN KEY (CompetenceId) REFERENCES Competences (Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS ResourceCompetences (
    ResourceId INT NOT NULL,
    CompetenceId INT NOT NULL,
    PRIMARY KEY (ResourceId, CompetenceId),
    CONSTRAINT FK_ResourceCompetences_Resources FOREIGN KEY (ResourceId) REFERENCES Resources (Id) ON DELETE CASCADE,
    CONSTRAINT FK_ResourceCompetences_Competences FOREIGN KEY (CompetenceId) REFERENCES Competences (Id) ON DELETE CASCADE
);

-- Koblingstabell mellom behov og ressurser. Primærnøkkelen (NeedId, ResourceId) gjør at samme ressurs
-- bare kan tildeles et behov én gang. ON DELETE CASCADE sletter tildelingene når behovet eller ressursen slettes.
CREATE TABLE IF NOT EXISTS Allocations (
    NeedId INT NOT NULL,
    ResourceId INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    AllocatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (NeedId, ResourceId),
    CONSTRAINT FK_Allocations_Needs FOREIGN KEY (NeedId) REFERENCES Needs (Id) ON DELETE CASCADE,
    CONSTRAINT FK_Allocations_Resources FOREIGN KEY (ResourceId) REFERENCES Resources (Id) ON DELETE CASCADE
);

-- Name må være lik Label i ResourceCategory.cs.
-- INSERT IGNORE hopper over kategorier som finnes fra før, så skriptet tåler å kjøres ved hver oppstart.
INSERT IGNORE INTO Categories (Name, Description) VALUES
    ('Mat', 'Mat og matutdeling'),
    ('Vann', 'Drikkevann'),
    ('Ly og varme', 'Husly, overnatting og varme'),
    ('Helse', 'Førstehjelp, medisin og helsehjelp'),
    ('Verktøy', 'Verktøy, utstyr og kjøretøy'),
    ('Strøm og lading', 'Aggregat, nødstrøm og lading'),
    ('Informasjon', 'Informasjonspunkter'),
    ('Annet', 'Det som ikke passer i de andre kategoriene');
