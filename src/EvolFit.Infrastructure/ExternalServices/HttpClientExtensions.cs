using System.Net.Http.Headers;
using EvolFit.Infrastructure.ExternalServices.TinyFn;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace EvolFit.Infrastructure.ExternalServices;

public static class HttpClientExtensions
{
    public static IServiceCollection AddExternalHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ---------- TinyFn ----------
        services.Configure<TinyFnOptions>(configuration.GetSection("ExternalApis:TinyFn"));

        services
            .AddHttpClient<EvolFit.Application.Features.TinyFn.Interfaces.ITinyFnHealthClient,
                TinyFnHealthClient>((sp, client) =>
                {
                    var opts = sp.GetRequiredService<IOptions<TinyFnOptions>>().Value;
                    client.BaseAddress = new Uri(opts.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(opts.TimeOutSeconds);

                    if (!string.IsNullOrWhiteSpace(opts.ApiKey))
                    {
                        client.DefaultRequestHeaders.Remove("X-API-Key");
                        client.DefaultRequestHeaders.Add("X-API-Key", opts.ApiKey);
                    }

                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        // ---------- wger ----------
        services.Configure<EvolFit.Infrastructure.ExternalServices.Wger.WgerOptions>(
            configuration.GetSection("ExternalApis:Wger"));

        services
            .AddHttpClient<EvolFit.Application.Features.Wger.Interfaces.IWgerExerciseClient,
                EvolFit.Infrastructure.ExternalServices.Wger.WgerExerciseClient>((sp, client) =>
                {
                    var opts = sp.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<
                    EvolFit.Infrastructure.ExternalServices.Wger.WgerOptions>>().Value;

                    client.BaseAddress = new Uri(opts.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(opts.TimeOutSeconds);

                    client.DefaultRequestHeaders.Add("User-Agent", "EvolFit/1.0");
                    client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    // 3 tentativas com backoff exponencial (1s, 2s, 4s)
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)));

    // Circuit breaker: abre após 5 falhas consecutivas, fica aberto 60s
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(60));
}
