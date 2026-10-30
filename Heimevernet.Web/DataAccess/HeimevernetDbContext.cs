using Heimevernet.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Web.DataAccess;

// Koblingen mellom C#-koden og databasen.
// IdentityDbContext arver fra DbContext og legger til tabellene Identity trenger:
// AspNetUsers, AspNetRoles, AspNetUserRoles (hvilken bruker har hvilken rolle) m.fl.
public class HeimevernetDbContext(DbContextOptions<HeimevernetDbContext> options)
    : IdentityDbContext<IdentityUser, IdentityRole, string>(options)
{
    // legg til database entiteter her
    public DbSet<Resource> Resources => Set<Resource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Må kalles først, ellers blir ikke Identity-tabellene satt opp.
        base.OnModelCreating(modelBuilder);

        // MariaDB tillater maks 3072 bytes i en nøkkel. Med utf8mb4 (4 bytes per tegn) blir
        // to kolonner på 255 tegn for lange, så vi korter ned kolonnene som inngår i sammensatte nøkler.
        modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.Property(login => login.LoginProvider).HasMaxLength(128);
            entity.Property(login => login.ProviderKey).HasMaxLength(128);
        });
        modelBuilder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.Property(token => token.LoginProvider).HasMaxLength(128);
            entity.Property(token => token.Name).HasMaxLength(128);
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.ToTable("Resources");
            entity.HasKey(resource => resource.Id);
            entity.Property(resource => resource.Name).HasMaxLength(200).IsRequired();
            entity.Property(resource => resource.Description).HasMaxLength(2000).IsRequired();
            entity.Property(resource => resource.Type).HasMaxLength(100).IsRequired();
            entity.Property(resource => resource.Address).HasMaxLength(200).IsRequired(false);
            entity.Property(resource => resource.City).HasMaxLength(100).IsRequired(false);
            entity.Property(resource => resource.ZipCode).HasMaxLength(6).IsRequired(false);
            entity.Property(resource => resource.OwnerTelephoneNumber).HasMaxLength(20).IsRequired(false);
        });
    }
}
