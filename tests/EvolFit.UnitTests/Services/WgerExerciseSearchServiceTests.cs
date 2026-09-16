using EvolFit.Application.Common;
using EvolFit.Application.Features.Wger.Interfaces;
using EvolFit.Core.Entities;
using EvolFit.Infrastructure.ExternalServices.Wger;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace EvolFit.UnitTests.Services;

public class WgerExerciseSearchServiceTests
{
    private readonly IWgerExerciseClient _wger = Substitute.For<IWgerExerciseClient>();
    private readonly IWgerExerciseCacheRepository _cache = Substitute.For<IWgerExerciseCacheRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private WgerExerciseSearchService CreateSut() =>
        new(_wger, _cache, _uow, NullLogger<WgerExerciseSearchService>.Instance);

    [Fact]
    public void RemoveDiacritics_ShouldStripAccents()
    {
        WgerExerciseSearchService.RemoveDiacritics("flexão do bíceps").Should().Be("flexao do biceps");
    }

    [Fact]
    public async Task Search_WhenCatalogFresh_ShouldSkipSync()
    {
        _cache.MaxCatalogExpiresAtAsync(Arg.Any<CancellationToken>())
              .Returns(DateTime.UtcNow.AddDays(1));
        _cache.SearchByTermsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
              .Returns(new List<WgerExerciseCache>());

        var sut = CreateSut();

        await sut.SearchAsync("bench", "all");

        await _wger.DidNotReceive().GetCatalogAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_WhenCatalogStale_ShouldSyncThenSearch()
    {
        var catalog = new List<Application.Features.Wger.DTOs.ExerciseCatalogItem>
        {
            new(530, "Run - Treadmill", "Esteira", null, null, "Cardio", 15, null,
                new List<string>(), new List<string>(), new List<string>()),
        };

        _cache.MaxCatalogExpiresAtAsync(Arg.Any<CancellationToken>())
              .Returns((DateTime?)null);
        _wger.GetCatalogAsync(Arg.Any<CancellationToken>()).Returns(catalog);
        _cache.SearchByTermsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
              .Returns(new List<WgerExerciseCache>());
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var sut = CreateSut();

        await sut.SearchAsync("esteira", "all");

        await _cache.Received(1).RemoveCatalogEntriesAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).AddAsync(
            Arg.Is<WgerExerciseCache>(e => e.WgerExerciseId == 530 && e.Name == "Run - Treadmill" && e.NamePt == "Esteira"),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_PortugueseTerm_ShouldExpandToEnglishKeywords()
    {
        IReadOnlyCollection<string>? requestedTerms = null;

        _cache.MaxCatalogExpiresAtAsync(Arg.Any<CancellationToken>())
              .Returns(DateTime.UtcNow.AddDays(1));
        _cache.SearchByTermsAsync(
                Arg.Do<IReadOnlyCollection<string>>(t => requestedTerms = t), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
              .Returns(new List<WgerExerciseCache>());

        var sut = CreateSut();

        await sut.SearchAsync("esteira", "all");

        requestedTerms.Should().NotBeNull();
        requestedTerms.Should().Contain("treadmill");
    }

    [Fact]
    public async Task Search_PortugueseMode_ShouldReturnPtName()
    {
        var cached = WgerExerciseCache.Create(
            530, "Run - Treadmill", "desc", "Cardio", "[]", null, null,
            TimeSpan.FromDays(7), categoryId: 15, namePt: "Esteira");

        _cache.MaxCatalogExpiresAtAsync(Arg.Any<CancellationToken>())
              .Returns(DateTime.UtcNow.AddDays(1));
        _cache.SearchByTermsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
              .Returns(new List<WgerExerciseCache> { cached });

        var sut = CreateSut();

        var res = await sut.SearchAsync("esteira", "portuguese");

        var result = res.Results.Should().ContainSingle().Subject;
        result.Id.Should().Be(530);
        result.Name.Should().Be("Esteira");
    }

    [Fact]
    public async Task Search_EnglishMode_ShouldReturnEnNameEvenWhenPtExists()
    {
        var cached = WgerExerciseCache.Create(
            530, "Run - Treadmill", "desc", "Cardio", "[]", null, null,
            TimeSpan.FromDays(7), categoryId: 15, namePt: "Esteira");

        _cache.MaxCatalogExpiresAtAsync(Arg.Any<CancellationToken>())
              .Returns(DateTime.UtcNow.AddDays(1));
        _cache.SearchByTermsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
              .Returns(new List<WgerExerciseCache> { cached });

        var sut = CreateSut();

        var res = await sut.SearchAsync("treadmill", "english");

        res.Results.Should().ContainSingle().Which.Name.Should().Be("Run - Treadmill");
    }
}