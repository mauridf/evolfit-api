using System.Net;
using System.Text;
using EvolFit.Infrastructure.ExternalServices.Wger;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EvolFit.IntegrationTests;

/// <summary>
/// Testes de mapeamento do WgerExerciseClient com HTTP stubado
/// (não depende do Postgres/coleção).
/// </summary>
public class WgerExerciseClientTests
{
    [Fact]
    public async Task GetExercisesByEquipmentAsync_ShouldBuildUrlAndMapResults()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                { "results": [ {
                    "id": 8,
                    "translations": [ { "language": 2, "name": "Barbell Curl", "description": "desc" } ],
                    "category": { "id": 14, "name": "Arms" },
                    "muscles": [ { "id": 1, "name": "Biceps Brachii" } ]
                } ] }
                """, Encoding.UTF8, "application/json")
        });

        var client = CreateClient(handler);

        var res = await client.GetExercisesByEquipmentAsync(1);

        res.Results.Should().ContainSingle(r => r.Id == 8 && r.Name == "Barbell Curl");
        res.Count.Should().Be(1);
        handler.RequestedUrls.Should().ContainSingle(u => u.Contains("equipment=1"));
    }

    [Fact]
    public async Task GetMusclesAsync_ShouldMapMuscleDtos()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                { "results": [ { "id": 2, "name": "Anterior Deltoid", "name_en": "Anterior Deltoid" } ] }
                """, Encoding.UTF8, "application/json")
        });

        var client = CreateClient(handler);

        var res = await client.GetMusclesAsync();

        res.Results.Should().ContainSingle(m => m.Id == 2 && m.Name == "Anterior Deltoid");
        handler.RequestedUrls.Should().ContainSingle().Which.Should().Contain("muscle/?limit=200");
    }

    [Fact]
    public async Task GetCategoriesAsync_ShouldMapCategoryDtos()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                { "results": [ { "id": 15, "name": "Cardio" } ] }
                """, Encoding.UTF8, "application/json")
        });

        var client = CreateClient(handler);

        var res = await client.GetCategoriesAsync();

        res.Results.Should().ContainSingle(c => c.Id == 15 && c.Name == "Cardio");
        handler.RequestedUrls.Should().ContainSingle().Which.Should().Contain("exercisecategory/?limit=200");
    }

    private static WgerExerciseClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://wger.de/api/v2/") },
            Options.Create(new WgerOptions()),
            NullLogger<WgerExerciseClient>.Instance);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public List<string> RequestedUrls { get; } = new();

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
            _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUrls.Add(request.RequestUri!.ToString());
            return Task.FromResult(_responder(request));
        }
    }
}