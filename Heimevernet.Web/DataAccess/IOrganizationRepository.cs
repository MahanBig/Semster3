using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

// Organisasjonene et behov kan høre til (nedtrekkslisten i behovsskjemaet).
public interface IOrganizationRepository
{
    IReadOnlyList<Organization> GetAll();
}
