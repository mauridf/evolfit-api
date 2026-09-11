using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EvolFit.Application.Features.Auth.DTOs;
using EvolFit.Application.Features.Health.DTOs;
using EvolFit.IntegrationTests.Fixtures;
using FluentAssertions;

namespace EvolFit.IntegrationTests;

[Collection("Postgres collection")]
public class HealthCrossUserTests
{
    private readonly HttpClient _client;

    public HealthCrossUserTests(PostgresFixture fixture)
    {
        var factory = new EvolFitWebAppFactory(fixture.ConnectionString);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UserB_CannotAccess_UserA_Metric()
    {
        // --- User A cria uma medição ---
        var tokenA = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        var createResp = await _client.PostAsJsonAsync("/api/health/metrics",
            new CreateHealthMetricRequest(75.5m, 180m, "male", 28, "moderate"));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var metric = await createResp.Content.ReadFromJsonAsync<HealthMetricResponse>();

        // --- User B tenta acessar a métrica do A ---
        var tokenB = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var getResp = await _client.GetAsync($"/api/health/metrics/{metric!.Id}");

        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var req = new RegisterRequest(
            $"u{suffix}", $"u{suffix}@evolfit.test", "S3nh@F0rte!", "Test", null);

        var r = await _client.PostAsJsonAsync("/api/auth/register", req);
        r.EnsureSuccessStatusCode();

        var body = await r.Content.ReadFromJsonAsync<RegisterResponse>();
        return body!.AccessToken;
    }
}
