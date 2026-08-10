using System.Diagnostics;

using MediatR;

using Microsoft.Extensions.Logging;

namespace SopmineWorkshop.Application.Common.Behaviours;

public class PerformanceBehaviour<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<TRequest> _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var timer = Stopwatch.StartNew();

        var response = await next(ct);

        timer.Stop();

        var elapsedMilliseconds = timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogWarning(
                "Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@Request}",
                requestName,
                elapsedMilliseconds,
                request);
        }

        return response;
    }
}
