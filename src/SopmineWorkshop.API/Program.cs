using System.IO.Compression;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

using SopmineWorkshop.API.Common;
using SopmineWorkshop.Domain.Identity;
using SopmineWorkshop.Infrastructure.Data;
using SopmineWorkshop.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);
var allowedCorsOrigins = GetConfiguredOrigins(builder.Configuration, "Cors:AllowedOrigins");
var trustForwardedHeaders = builder.Configuration.GetValue<bool>("Hosting:TrustForwardedHeaders");

ProductionConfigurationValidator.Validate(
    builder.Configuration,
    builder.Environment.IsProduction(),
    allowedCorsOrigins);

if (trustForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

builder.Services.AddCustomApiVersioning();
builder.Services.AddApiDocumentation();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(ApiRateLimitPolicies.Login, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy(ApiCachePolicies.BusinessList, policy => policy
        .Cache()
        .Expire(TimeSpan.FromSeconds(30))
        .SetVaryByHeader("Authorization")
        .SetVaryByQuery("*"));

    options.AddPolicy(ApiCachePolicies.ReferenceData, policy => policy
        .Cache()
        .Expire(TimeSpan.FromMinutes(5))
        .SetVaryByHeader("Authorization")
        .SetVaryByQuery("*"));
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(ApiAuthorizationPolicies.PurchasesOnly, policy =>
        policy.RequireRole(nameof(Role.Admin)));
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("Client", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod();

        if (allowedCorsOrigins.Length > 0)
        {
            policy.WithOrigins(allowedCorsOrigins);
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.SetIsOriginAllowed(_ => false);
        }
    });
});

var app = builder.Build();

if (trustForwardedHeaders)
{
    app.UseForwardedHeaders();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "SopmineWorkshop API V1");
    });
}
else
{
    if (builder.Configuration.GetValue("Hosting:UseHsts", true))
    {
        app.UseHsts();
    }
}

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    var identitySeeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
    await identitySeeder.SeedAsync();
}

if (!app.Environment.IsDevelopment() &&
    builder.Configuration.GetValue("Hosting:UseHttpsRedirection", true))
{
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

    await next();
});

UseLegacyFrontendRedirects(app);
UseFrontendStaticFiles(app);

app.UseResponseCompression();
app.UseRouting();
app.UseCors("Client");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();


app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapGet("/api/status", () => Results.Ok(new
{
    service = "SopmineWorkshop.API",
    status = "OK"
})).AllowAnonymous();

app.Run();

static void UseLegacyFrontendRedirects(WebApplication app)
{
    app.Use(async (context, next) =>
    {
        if (HttpMethods.IsGet(context.Request.Method) &&
            !Path.HasExtension(context.Request.Path.Value) &&
            TryGetLegacyFrontendRedirect(context.Request.Path, context.Request.Query, out var destination))
        {
            context.Response.Redirect(destination, permanent: false);
            return;
        }

        await next();
    });
}

static bool TryGetLegacyFrontendRedirect(
    PathString requestPath,
    IQueryCollection query,
    out string destination)
{
    destination = string.Empty;
    var path = (requestPath.Value ?? string.Empty)
        .Replace("/index.html", "/", StringComparison.OrdinalIgnoreCase)
        .TrimEnd('/')
        .ToLowerInvariant();

    static bool Matches(string pathValue, string root)
        => pathValue.Equals($"/{root}", StringComparison.Ordinal) ||
           pathValue.StartsWith($"/{root}/", StringComparison.Ordinal);

    static string SafeId(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : Uri.EscapeDataString(value.Trim());

    var id = SafeId(query["id"].FirstOrDefault());

    if (Matches(path, "dashboard")) destination = "/Product/#products";
    else if (Matches(path, "produit"))
    {
        destination = id.Length > 0
            ? $"/Product/#product/{id}"
            : query["openProductModal"] == "1" ? "/Product/#product-new" : "/Product/#products";
    }
    else if (Matches(path, "addsupplier")) destination = "/Supplier/#supplier-new";
    else if (Matches(path, "fournisseur"))
    {
        destination = id.Length > 0
            ? $"/Supplier/#supplier/{id}"
            : query["openSupplierModal"] == "1" ? "/Supplier/#supplier-new" : "/Supplier/#suppliers";
    }
    else if (Matches(path, "client"))
    {
        destination = id.Length > 0
            ? $"/Client/index.html#client/{id}"
            : query["openClientModal"] == "1" ? "/Client/index.html#client-new" : "/Client/index.html#clients";
    }
    else if (Matches(path, "documents"))
    {
        var purchase = string.Equals(query["nature"].FirstOrDefault(), "achat", StringComparison.OrdinalIgnoreCase);
        var area = purchase ? "purchases" : "sales";
        var singular = purchase ? "purchase" : "sale";
        var requestedType = query["type"].FirstOrDefault()?.ToLowerInvariant() ?? string.Empty;
        var allowedTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "boncommande", "bonreception", "bonlivraison", "devis", "facture", "avoir"
        };
        var type = allowedTypes.Contains(requestedType) ? requestedType : purchase ? "boncommande" : "devis";
        var documentId = SafeId(query["id"].FirstOrDefault() ?? query["invoiceId"].FirstOrDefault());
        var create = query["openDocumentModal"] == "1" || path.EndsWith("/editor", StringComparison.Ordinal) || path.EndsWith("/editor.html", StringComparison.Ordinal);
        destination = documentId.Length > 0
            ? $"/Document/#{singular}/{documentId}"
            : create ? $"/Document/#{singular}-new/{type}" : $"/Document/#{area}/{type}";
    }
    else if (Matches(path, "familles") || Matches(path, "unitesmesure"))
        destination = "/Reference/#references";
    else if (Matches(path, "parametres")) destination = "/Settings/#settings/users";

    return destination.Length > 0;
}

static void UseFrontendStaticFiles(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        var frontendRoot = Path.GetFullPath(
            Path.Combine(app.Environment.ContentRootPath, "..", "..", "Frontend"));

        if (Directory.Exists(frontendRoot))
        {
            var fileProvider = new PhysicalFileProvider(frontendRoot);

            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = fileProvider,
            });
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = fileProvider,
            });

            return;
        }
    }

    app.UseDefaultFiles();
    app.UseStaticFiles();
}

static string[] GetConfiguredOrigins(IConfiguration configuration, string sectionName)
    => configuration
        .GetSection(sectionName)
        .Get<string[]>()?
        .Select(origin => origin.Trim().TrimEnd('/'))
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? [];

public partial class Program { }
