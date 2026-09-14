using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EvolFit.Application.Features.Auth.DTOs;
using EvolFit.IntegrationTests.Fixtures;
using FluentAssertions;

namespace EvolFit.IntegrationTests;

[Collection("Postgres collection")]
public class AuthIntegrationTests
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(PostgresFixture fixture)
    {
        var factory = new EvolFitWebAppFactory(fixture.ConnectionString);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullAuthFlow_ShouldSucceed()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var register = new RegisterRequest(
            $"user{suffix}",
            $"user{suffix}@evolfit.test",
            "S3nh@F0rte!",
            "Test User",
            new DateOnly(1998, 5, 15));

        // 1. Register
        var r1 = await _client.PostAsJsonAsync("/api/auth/register", register);
        r1.StatusCode.Should().Be(HttpStatusCode.Created);

        var reg = await r1.Content.ReadFromJsonAsync<RegisterResponse>();
        reg!.AccessToken.Should().NotBeNullOrEmpty();

        // 2. Login
        var r2 = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(register.Email, "S3nh@F0rte!"));
        r2.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await r2.Content.ReadFromJsonAsync<LoginResponse>();
        login!.AccessToken.Should().NotBeNullOrEmpty();

        // 3. Profile
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login.AccessToken);

        var r3 = await _client.GetAsync("/api/auth/profile");
        r3.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Refresh
        _client.DefaultRequestHeaders.Authorization = null;
        var r4 = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest(login.RefreshToken));
        r4.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Replay do refresh antigo → 409
        var r5 = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest(login.RefreshToken));
        r5.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturn422()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var req = new RegisterRequest(
            $"weak{suffix}", $"weak{suffix}@evolfit.test", "abc123", "Weak User", null);

        var r = await _client.PostAsJsonAsync("/api/auth/register", req);
        r.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
