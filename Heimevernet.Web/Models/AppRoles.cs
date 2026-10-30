namespace Heimevernet.Web.Models;

// Navnene på rollene samlet ett sted, så vi slipper skrivefeil som "admin" vs "Admin".
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string User = "Bruker";

    public static readonly IReadOnlyList<string> All = [Admin, User];
}
