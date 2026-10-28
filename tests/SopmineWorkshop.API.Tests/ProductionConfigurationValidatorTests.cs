using Microsoft.Extensions.Configuration;

using SopmineWorkshop.API.Common;

using Xunit;

namespace SopmineWorkshop.API.Tests;

public sealed class ProductionConfigurationValidatorTests
{
    [Fact]
    public void Validate_ProductionWithValidConfiguration_DoesNotThrow()
    {
        var exception = Record.Exception(() =>
            ProductionConfigurationValidator.Validate(
                ValidConfiguration(),
                isProduction: true,
                ["https://shop.example.com"]));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("admin@example.com")]
    [InlineData("REPLACE_WITH_ADMIN_EMAIL")]
    public void Validate_MissingOrExampleAdminEmail_Throws(string? email)
    {
        var configuration = ValidConfiguration(("DefaultAdmin:Email", email));

        Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(
                configuration,
                isProduction: true,
                ["https://shop.example.com"]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoNumberHere!")]
    [InlineData("NoSpecial123")]
    [InlineData("REPLACE_WITH_A_STRONG_ADMIN_PASSWORD")]
    public void Validate_MissingPlaceholderOrWeakAdminPassword_Throws(string? password)
    {
        var configuration = ValidConfiguration(("DefaultAdmin:Password", password));

        Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(
                configuration,
                isProduction: true,
                ["https://shop.example.com"]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Owner")]
    public void Validate_InvalidAdminRole_Throws(string role)
    {
        var configuration = ValidConfiguration(("DefaultAdmin:Role", role));

        Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(
                configuration,
                isProduction: true,
                ["https://shop.example.com"]));
    }

    [Fact]
    public void Validate_InvalidJwtSecret_Throws()
    {
        var configuration = ValidConfiguration(("JwtSettings:Secret", "short"));

        Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(
                configuration,
                isProduction: true,
                ["https://shop.example.com"]));
    }

    [Fact]
    public void Validate_LocalDatabaseConnection_Throws()
    {
        var configuration = ValidConfiguration((
            "ConnectionStrings:DefaultConnection",
            "Server=localhost;Database=SopmineShop;User Id=sa;Password=ValidSql1!Password"));

        Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(
                configuration,
                isProduction: true,
                ["https://shop.example.com"]));
    }

    [Fact]
    public void Validate_InsecureCorsOrigin_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(
                ValidConfiguration(),
                isProduction: true,
                ["http://shop.example.com"]));
    }

    [Fact]
    public void Validate_NonProduction_SkipsProductionRequirements()
    {
        var exception = Record.Exception(() =>
            ProductionConfigurationValidator.Validate(
                new ConfigurationBuilder().Build(),
                isProduction: false,
                []));

        Assert.Null(exception);
    }

    private static IConfiguration ValidConfiguration(
        params (string Key, string? Value)[] overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Server=db,1433;Database=SopmineShop;User Id=sa;Password=ValidSql1!Password;TrustServerCertificate=True",
            ["JwtSettings:Issuer"] = "SopmineShopManagement",
            ["JwtSettings:Audience"] = "SopmineShopManagementClient",
            ["JwtSettings:Secret"] = "ValidJwtSecret-1234567890-ABCDEFGH",
            ["DefaultAdmin:Email"] = "owner@sopmine.test",
            ["DefaultAdmin:Password"] = "ValidAdmin1!Password",
            ["DefaultAdmin:Role"] = "Admin"
        };

        foreach (var (key, value) in overrides)
        {
            values[key] = value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
