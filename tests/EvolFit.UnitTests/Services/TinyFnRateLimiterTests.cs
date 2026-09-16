using EvolFit.Application.Features.TinyFn.Exceptions;
using EvolFit.Application.Features.TinyFn.Interfaces;
using EvolFit.Infrastructure.ExternalServices.TinyFn;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EvolFit.UnitTests.Services;

public class TinyFnRateLimiterTests
{
    private readonly FakeTimeProvider _timeProvider = new(new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void TryConsumeRequest_ShouldAllowUpToMax()
    {
        var limiter = CreateLimiter(3);
        for (var i = 0; i < 3; i++)
            limiter.TryConsumeRequest().Should().BeTrue($"requisição {i + 1} deve ser permitida");
    }

    [Fact]
    public void TryConsumeRequest_AfterMax_ShouldReject()
    {
        var limiter = CreateLimiter(3);
        for (var i = 0; i < 3; i++) limiter.TryConsumeRequest();

        limiter.TryConsumeRequest().Should().BeFalse();
        limiter.TryConsumeRequest().Should().BeFalse();
    }

    [Fact]
    public void GetRemainingRequests_ShouldDecrease()
    {
        var limiter = CreateLimiter(5);
        limiter.GetRemainingRequests().Should().Be(5);

        limiter.TryConsumeRequest();
        limiter.GetRemainingRequests().Should().Be(4);

        for (var i = 0; i < 4; i++) limiter.TryConsumeRequest();
        limiter.GetRemainingRequests().Should().Be(0);
    }

    [Fact]
    public void ShouldResetOnNewDay()
    {
        var limiter = CreateLimiter(3);
        limiter.TryConsumeRequest();
        limiter.TryConsumeRequest();

        _timeProvider.Now = _timeProvider.Now.AddDays(1);

        limiter.TryConsumeRequest().Should().BeTrue("dia seguinte deve resetar");
        limiter.GetRemainingRequests().Should().Be(2);
    }

    [Fact]
    public void ShouldNotResetWithinSameDay()
    {
        var limiter = CreateLimiter(3);
        limiter.TryConsumeRequest();

        _timeProvider.Now = _timeProvider.Now.AddHours(8);

        limiter.GetRemainingRequests().Should().Be(2);
    }

    [Fact]
    public void MaxRequestsPerDay_ShouldReflectConfig() =>
        CreateLimiter(10).MaxRequestsPerDay.Should().Be(10);

    private TinyFnRateLimiter CreateLimiter(int max) =>
        new(Options.Create(new TinyFnOptions { MaxRequestsPerDay = max }), _timeProvider);

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}