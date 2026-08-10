using Asp.Versioning;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        string[] versions = ["v1"];

        foreach (var version in versions)
        {
            services.AddOpenApi(version, options =>
            {
                options.AddDocumentTransformer<VersionInfoTransformer>();
            });
        }

        return services;
    }
}

internal sealed class VersionInfoTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var version = context.DocumentName;

        document.Info.Version = version;
        document.Info.Title = $"SopmineWorkshop API {version}";

        return Task.CompletedTask;
    }
}
