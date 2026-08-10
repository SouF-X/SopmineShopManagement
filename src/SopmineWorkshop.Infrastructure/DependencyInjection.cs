using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Invoices;
using SopmineWorkshop.Infrastructure.Data;
using SopmineWorkshop.Infrastructure.Identity;
using SopmineWorkshop.Infrastructure.Invoices;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        ArgumentNullException.ThrowIfNull(connectionString);

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)));

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IDocumentReferenceGenerator, DocumentReferenceGenerator>();

        services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 4;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var jwtSettings = GetJwtSettings(configuration);

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Secret))
            };
        });

        services.AddTransient<IIdentityService, IdentityService>();
        services.AddTransient<ITokenProvider, TokenProvider>();
        services.AddTransient<IInvoiceExtractionService, OpenAIInvoiceExtractionService>();
        services.AddHybridCache(options => options.DefaultEntryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(10),
            LocalCacheExpiration = TimeSpan.FromSeconds(30),
        });
        services.AddScoped<IdentitySeeder>();

        return services;
    }

    private static JwtSettings GetJwtSettings(IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var secret = jwtSettings["Secret"];

        if (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(secret) ||
            Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException(
                "JwtSettings must include Issuer, Audience, and a Secret of at least 32 bytes.");
        }

        return new JwtSettings(issuer, audience, secret);
    }

    private sealed record JwtSettings(string Issuer, string Audience, string Secret);
}
