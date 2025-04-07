using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;

namespace JoueursTennisTests
{
    public class PlayerApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public PlayerApiTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetPlayers_Should_ReturnSortedList()
        {
            var response = await _client.GetAsync("/getPlayers");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var players = json.GetProperty("players").EnumerateArray().ToList();
            var ranks = players.Select(p => p.GetProperty("data").GetProperty("rank").GetInt32());

            ranks.Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task GetPlayer_Should_ReturnCorrectPlayer()
        {
            var response = await _client.GetAsync("/getPlayer/17");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var player = await response.Content.ReadFromJsonAsync<JsonElement>();
            player.GetProperty("id").GetInt32().Should().Be(17);
        }

        [Fact]
        public async Task GetPlayer_Should_ReturnNotFound_ForUnknownId()
        {
            var response = await _client.GetAsync("/getPlayer/9999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetStats_Should_ReturnAllFields()
        {
            var response = await _client.GetAsync("/getStats");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            json.TryGetProperty("bestCountry", out _).Should().BeTrue();
            json.TryGetProperty("bestWinRatio", out _).Should().BeTrue();
            json.TryGetProperty("averageIMC", out _).Should().BeTrue();
            json.TryGetProperty("medianHeight", out _).Should().BeTrue();
        }
    }
}
