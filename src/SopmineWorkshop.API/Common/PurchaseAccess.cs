using System.Security.Claims;

using SopmineWorkshop.Domain.Enums;
using SopmineWorkshop.Domain.Identity;

namespace SopmineWorkshop.API.Common;

public static class PurchaseAccess
{
    public static bool CanAccessPurchases(ClaimsPrincipal user)
        => user.IsInRole(nameof(Role.Admin));

    public static bool IsRestricted(ClaimsPrincipal user, InvoiceNature nature)
        => nature == InvoiceNature.Achat && !CanAccessPurchases(user);
}
