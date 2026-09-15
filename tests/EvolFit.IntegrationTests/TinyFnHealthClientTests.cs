using System.Net;
using System.Text;
using EvolFit.Application.Features.TinyFn.Exceptions;
using EvolFit.Application.Features.TinyFn.Interfaces;
using EvolFit.Infrastructure.ExternalServices.TinyFn;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EvolFit.IntegrationTests;

/// <summary>
/// Testes de mapeamento do TinyFnHealthClient com HTTP stubado
/// (não depende do Postgres/coleção).
/// </summary>
public class TinyFnHealthClientTests
{
    [Fact]
    public async Task CalculateBmiAsync_ShouldMapBmiAndMakeSingleHttpRequest()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                { "bmi": 24.5, "category": "Normal weight" }
                """, Encoding.UTF8, "application/json")
        });

        var client = CreateClient(handler);
        var result = await client.CalculateBmiAsync(new (75.0, 175.0));

        result.Bmi.Should().Be(24.5m);
        result.Category.Should().Be("Normal weight");
        handler.RequestedUrls.Should().ContainSingle().Which.Should().Contain("bmi?weight_kg=75&height_cm=175");
    }

    [Fact]
    public async Task CalculateTdeeAsync_ShouldMapFullResponseIncludingMacros()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                { "tdee": 2500, "bmr": 1700, "activity_multiplier": 1.47,
                  "activity_level": "moderate", "unit": "kcal",
                  "macros_suggestion": { "protein_g": 150, "carbs_g": 280, "fat_g": 70 } }
                """, Encoding.UTF8, "application/json")
        });

        var client = CreateClient(handler);
        var result = await client.CalculateTdeeAsync(new (80, 180, 30, "male", "moderate"));

        result.Tdee.Should().Be(2500);
        result.Bmr.Should().Be(1700);
        result.MacrosSuggestion.Should().NotBeNull();
        result.MacrosSuggestion!.ProteinG.Should().Be(150);

        handler.RequestedUrls.Should().ContainSingle().Which.Should()
            .Contain("tdee?weight=80&height=180&age=30&gender=male&activity=moderate");
    }

    [Fact]
    public async Task CalculateBmiAsync_WhenRateLimitExhausted_ShouldThrowAndMakeNoHttpCalls()
    {
        var limiter = new TinyFnRateLimiter(
            Options.Create(new TinyFnOptions { MaxRequestsPerDay = 2 }));

        // Consumir as 2 requisições permitidas
        limiter.TryConsumeRequest().Should().BeTrue();
        limiter.TryConsumeRequest().Should().BeTrue();

        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"bmi": 24, "category": "Normal"}""",
                Encoding.UTF8, "application/json")
        });

        var client = CreateClient(handler, limiter);

        await client.Invoking(c => c.CalculateBmiAsync(new (70, 170)))
            .Should().ThrowAsync<TinyFnRateLimitExceededException>();

        handler.RequestedUrls.Should().BeEmpty("nenhum HTTP deve ser disparado quando a cota esgota");
    }

    [Fact]
    public async Task XApiKeyHeader_ShouldBeSetWhenProvided()
    {
        var capturedHeaders = new Dictionary<string, string>();

        var handler = new StubHandler(req =>
        {
            foreach (var h in req.Headers)
                capturedHeaders[h.Key] = string.Join(", ", h.Value);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"bmi": 24, "category": "Normal"}""",
                    Encoding.UTF8, "application/json")
            };
        });

        var client = CreateClient(handler, new TinyFnRateLimiter(
            Options.Create(new TinyFnOptions { MaxRequestsPerDay = 10 })), "secret-api-key");

        await client.CalculateBmiAsync(new (70, 170));

        capturedHeaders.Should().ContainKey("X-API-Key");
        capturedHeaders["X-API-Key"].Should().Be("secret-api-key");
    }

    private static TinyFnHealthClient CreateClient(
        StubHandler handler,
        ITinyFnRateLimiter? rateLimiter = null,
        string? apiKey = null)
    {
        rateLimiter ??= new TinyFnRateLimiter(
            Options.Create(new TinyFnOptions { MaxRequestsPerDay = 10 }));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.tinyfn.io/v1/health/")
        };

        if (apiKey is not null)
            httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        return new TinyFnHealthClient(
            httpClient,
            Options.Create(new TinyFnOptions { ApiKey = apiKey ?? string.Empty }),
            rateLimiter,
            NullLogger<TinyFnHealthClient>.Instance);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<string> RequestedUrls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUrls.Add(request.RequestUri!.ToString());
            return Task.FromResult(responder(request));
        }
    }
}