using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

public interface IOrganizationRepository
{
    IReadOnlyList<Organization> GetAll();
}
