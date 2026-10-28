using Microsoft.EntityFrameworkCore;
using SopmineWorkshop.Infrastructure.Data;
using Xunit;

namespace SopmineWorkshop.Infrastructure.Tests;

public sealed class AppDbContextModelTests
{
    [Fact]
    public void Obsolete_product_draft_entity_is_not_mapped()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new AppDbContext(options);

        Assert.DoesNotContain(
            context.Model.GetEntityTypes(),
            entityType => entityType.ClrType.Name == "ProduitBrouillon");
    }
}
