using System.Text;

using Microsoft.Extensions.Configuration;

using SopmineWorkshop.Domain.Identity;

namespace SopmineWorkshop.API.Common;

public static class ProductionConfigurationValidator
{
    public static void Validate(
        IConfiguration configuration,
        bool isProduction,
        IReadOnlyCollection<string> allowedCorsOrigins)
    {
        if (!isProduction)
        {
            return;
        }

        ValidateJwt(configuration);
        ValidateConnectionString(configuration);
        ValidateCors(allowedCorsOrigins);
        ValidateDefaultAdmin(configuration);
    }

    private static void ValidateJwt(IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var secret = jwtSettings["Secret"];

        if (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(secret) ||
            Encoding.UTF8.GetByteCount(secret) < 32 ||
            IsPlaceholder(secret) ||
            secret.Contains("DEV_ONLY", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Production JwtSettings must include a real issuer, audience, and a random secret of at least 32 bytes.");
        }
    }

    private static void ValidateConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString) ||
            connectionString.Contains("Trusted_Connection=True", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Integrated Security=True", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Server=.;", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("YOUR_", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("REPLACE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Production ConnectionStrings:DefaultConnection must point to a real hosted SQL Server user/password connection.");
        }
    }

    private static void ValidateCors(IEnumerable<string> allowedCorsOrigins)
    {
        if (allowedCorsOrigins.Any(origin =>
                origin == "*" ||
                !Uri.TryCreate(origin, UriKind.Absolute, out var originUri) ||
                !string.Equals(originUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                origin.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
                origin.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "Production Cors:AllowedOrigins must contain only real HTTPS frontend origins.");
        }
    }

    private static void ValidateDefaultAdmin(IConfiguration configuration)
    {
        var email = configuration["DefaultAdmin:Email"];
        var password = configuration["DefaultAdmin:Password"];
        var role = configuration["DefaultAdmin:Role"] ?? nameof(Role.Admin);

        if (string.IsNullOrWhiteSpace(email) ||
            IsPlaceholder(email) ||
            string.Equals(email, "admin@example.com", StringComparison.OrdinalIgnoreCase) ||
            !email.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Production DefaultAdmin:Email must contain the real initial administrator email address.");
        }

        if (!IsValidIdentityPassword(password) || IsPlaceholder(password!))
        {
            throw new InvalidOperationException(
                "Production DefaultAdmin:Password must be at least 10 characters and include uppercase, lowercase, number, special character, and at least 4 unique characters.");
        }

        if (string.IsNullOrWhiteSpace(role) ||
            !Enum.TryParse<Role>(role, ignoreCase: true, out _))
        {
            throw new InvalidOperationException(
                $"Production DefaultAdmin:Role must be one of: {string.Join(", ", Enum.GetNames<Role>())}.");
        }
    }

    private static bool IsValidIdentityPassword(string? password)
        => !string.IsNullOrWhiteSpace(password) &&
           password.Length >= 10 &&
           password.Any(char.IsUpper) &&
           password.Any(char.IsLower) &&
           password.Any(char.IsDigit) &&
           password.Any(character => !char.IsLetterOrDigit(character)) &&
           password.Distinct().Count() >= 4;

    private static bool IsPlaceholder(string value)
        => value.StartsWith("REPLACE", StringComparison.OrdinalIgnoreCase);
}
