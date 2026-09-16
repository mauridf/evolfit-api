using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using EvolFit.Application.Features.Auth.DTOs;
using EvolFit.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;

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

    [Fact]
    public async Task Login_AfterFiveFailedAttempts_BlocksAccount()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"lock{suffix}@evolfit.test";
        const string password = "S3nh@F0rte!";

        var register = new RegisterRequest(
            $"lock{suffix}", email, password, "Lock User", null);
        var r0 = await _client.PostAsJsonAsync("/api/auth/register", register);
        r0.StatusCode.Should().Be(HttpStatusCode.Created);

        // 5 tentativas com senha errada → 401 (sem mensagem de bloqueio)
        for (var i = 0; i < 5; i++)
        {
            var r = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest(email, "senha-errada"));
            r.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var body = await r.Content.ReadAsStringAsync();
            body.Should().NotContain("bloquead");
        }

        // A 6ª tentativa, mesmo com a senha correta, é bloqueada (401 com aviso)
        var blocked = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, password));
        blocked.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var blockedBody = await blocked.Content.ReadAsStringAsync();
        blockedBody.Should().Contain("bloquead");
    }

    [Fact]
    public async Task PublicResponse_ShouldIncludeSecurityHeaders()
    {
        var r = await _client.GetAsync("/health");
        r.StatusCode.Should().Be(HttpStatusCode.OK);

        AssertSecurityHeaders(r);
    }

    [Fact]
    public async Task AuthenticatedResponse_ShouldHaveNoStoreAndSecurityHeaders()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var register = new RegisterRequest(
            $"hdr{suffix}", $"hdr{suffix}@evolfit.test", "S3nh@F0rte!", "Header User", null);
        var r0 = await _client.PostAsJsonAsync("/api/auth/register", register);
        r0.StatusCode.Should().Be(HttpStatusCode.Created);

        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(register.Email, "S3nh@F0rte!"));
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var r = await _client.GetAsync("/api/auth/profile");
        r.StatusCode.Should().Be(HttpStatusCode.OK);

        AssertSecurityHeaders(r);
        r.Headers.CacheControl.Should().NotBeNull();
        r.Headers.CacheControl!.NoStore.Should().BeTrue();
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ShouldReturn401()
    {
        var r = await _client.GetAsync("/api/workouts");
        r.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExpiredJwt_ShouldReturn401WithoutReissuing()
    {
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CreateExpiredToken());

        var r = await _client.GetAsync("/api/auth/profile");
        r.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await r.Content.ReadAsStringAsync();
        body.Should().NotContain("accessToken");
    }

    [Fact]
    public async Task Responses_ShouldNeverExposeTinyFnApiKey()
    {
        const string sentinel = "SENTINEL-tinyfn-key-do-not-leak";
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"leak{suffix}@evolfit.test";

        var register = new RegisterRequest(
            $"leak{suffix}", email, "S3nh@F0rte!", "Leak User", null);
        var r1 = await _client.PostAsJsonAsync("/api/auth/register", register);
        (await r1.Content.ReadAsStringAsync())
            .Should().NotContainEquivalentOf(sentinel);

        var r2 = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "S3nh@F0rte!"));
        (await r2.Content.ReadAsStringAsync())
            .Should().NotContainEquivalentOf(sentinel);

        var token = (await r2.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var r3 = await _client.GetAsync("/api/auth/profile");
        (await r3.Content.ReadAsStringAsync())
            .Should().NotContainEquivalentOf(sentinel);

        var r4 = await _client.GetAsync("/api/health/metrics");
        (await r4.Content.ReadAsStringAsync())
            .Should().NotContainEquivalentOf(sentinel);

        var r5 = await _client.GetAsync("/api/workouts");
        (await r5.Content.ReadAsStringAsync())
            .Should().NotContainEquivalentOf(sentinel);
    }

    private static string CreateExpiredToken()
    {
        var key = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("test_secret_evolfit_01234567890abcdef01234567890abcdef"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: "EvolFit",
            audience: "evolfit-api",
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(JwtRegisteredClaimNames.Sub, "1"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            notBefore: now.AddMinutes(-15),
            expires: now.AddMinutes(-5),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static void AssertSecurityHeaders(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("X-Content-Type-Options", out var xcto).Should().BeTrue();
        xcto!.Should().Contain("nosniff");

        response.Headers.TryGetValues("X-Frame-Options", out var xfo).Should().BeTrue();
        xfo!.Should().Contain("DENY");

        response.Headers.TryGetValues("Referrer-Policy", out var rp).Should().BeTrue();
        rp!.Should().Contain("strict-origin-when-cross-origin");

        response.Headers.TryGetValues("Permissions-Policy", out var pp).Should().BeTrue();
        pp!.Should().Contain("camera=(), microphone=(), geolocation=()");

        response.Headers.TryGetValues("Content-Security-Policy", out var csp).Should().BeTrue();
        csp!.Should().Contain("default-src 'self'");
    }
}
