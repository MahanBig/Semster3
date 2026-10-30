-- Lager alle tabellene appen trenger i MariaDB.
-- Kjøres hver gang appen starter (se DatabaseInitializer.cs).
-- "IF NOT EXISTS" gjør at ingenting skjer med tabeller som allerede finnes, så data blir ikke slettet.
-- Vil du endre en eksisterende tabell, må du skrive en ALTER TABLE-setning i tillegg.

-- Ressursene som vises på /ResourceHandler.
CREATE TABLE IF NOT EXISTS Resources (
    Id INT NOT NULL AUTO_INCREMENT,
    Name VARCHAR(200) NOT NULL,
    Description VARCHAR(2000) NOT NULL,
    Type VARCHAR(100) NOT NULL,
    Address VARCHAR(200) NULL,
    City VARCHAR(100) NULL,
    ZipCode VARCHAR(6) NULL,
    OwnerTelephoneNumber VARCHAR(20) NULL,
    PRIMARY KEY (Id)
);

-- Brukerne. Kolonnene tilsvarer egenskapene på IdentityUser.
-- "Normalized"-kolonnene er STORE BOKSTAVER, så søk ikke skiller på store og små bokstaver.
CREATE TABLE IF NOT EXISTS AspNetUsers (
    Id VARCHAR(255) NOT NULL,
    UserName VARCHAR(256) NULL,
    NormalizedUserName VARCHAR(256) NULL,
    Email VARCHAR(256) NULL,
    NormalizedEmail VARCHAR(256) NULL,
    EmailConfirmed BOOLEAN NOT NULL,
    PasswordHash LONGTEXT NULL,          -- Aldri selve passordet, bare en hash av det.
    SecurityStamp LONGTEXT NULL,         -- Endres når passordet endres, så gamle innlogginger blir ugyldige.
    ConcurrencyStamp LONGTEXT NULL,      -- Hindrer at to samtidige endringer overskriver hverandre.
    PhoneNumber LONGTEXT NULL,
    PhoneNumberConfirmed BOOLEAN NOT NULL,
    TwoFactorEnabled BOOLEAN NOT NULL,
    LockoutEnd DATETIME(6) NULL,         -- Lagres i UTC. Kontoen er låst til dette tidspunktet.
    LockoutEnabled BOOLEAN NOT NULL,
    AccessFailedCount INT NOT NULL,      -- Antall feil passord på rad.
    PRIMARY KEY (Id),
    UNIQUE INDEX UserNameIndex (NormalizedUserName),
    INDEX EmailIndex (NormalizedEmail)
);

-- Rollene, f.eks. Admin og Bruker.
CREATE TABLE IF NOT EXISTS AspNetRoles (
    Id VARCHAR(255) NOT NULL,
    Name VARCHAR(256) NULL,
    NormalizedName VARCHAR(256) NULL,
    ConcurrencyStamp LONGTEXT NULL,
    PRIMARY KEY (Id),
    UNIQUE INDEX RoleNameIndex (NormalizedName)
);

-- Koblingstabell: hvilken bruker har hvilken rolle (mange-til-mange).
-- ON DELETE CASCADE: slettes en bruker eller rolle, forsvinner koblingene automatisk.
CREATE TABLE IF NOT EXISTS AspNetUserRoles (
    UserId VARCHAR(255) NOT NULL,
    RoleId VARCHAR(255) NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES AspNetRoles (Id) ON DELETE CASCADE
);
