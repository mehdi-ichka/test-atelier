using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapGet("/getPlayers", async () =>
{
    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "players.json");

    var json = await File.ReadAllTextAsync(filePath);

    using var doc = JsonDocument.Parse(json);
    var players = doc.RootElement.GetProperty("players");

    var sortedPlayers = players.EnumerateArray()
        .Select(p => p.Clone())
        .OrderBy(p => p.GetProperty("data").GetProperty("rank").GetInt32())
        .ToList();

    return Results.Json(new { players = sortedPlayers });
});

app.MapGet("/getPlayer/{id:int}", async (int id) =>
{
    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "players.json");

    var json = await File.ReadAllTextAsync(filePath);
    using var doc = JsonDocument.Parse(json);
    var players = doc.RootElement.GetProperty("players");

    var player = players.EnumerateArray()
        .FirstOrDefault(p => p.GetProperty("id").GetInt32() == id);

    if (player.ValueKind == JsonValueKind.Undefined)
        return Results.NotFound($"Aucun joueur trouvé avec l’ID {id}");

    var playerClone = player.Clone();
    doc.Dispose();

    return Results.Json(playerClone);
});

app.MapGet("/getStats", async () =>
{
    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "players.json");

    var json = await File.ReadAllTextAsync(filePath);
    var doc = JsonDocument.Parse(json);
    var players = doc.RootElement.GetProperty("players");

    var heights = new List<int>();
    var imcs = new List<double>();
    var countryStats = new Dictionary<string, (int wins, int total)>();

    foreach (var player in players.EnumerateArray())
    {
        var data = player.GetProperty("data");
        var countryCode = player.GetProperty("country").GetProperty("code").GetString();
        var weight = data.GetProperty("weight").GetInt32();
        var height = data.GetProperty("height").GetInt32();
        var last = data.GetProperty("last").EnumerateArray().Select(e => e.GetInt32()).ToList();

        heights.Add(height);

        double tailleM = height / 100.0;
        double poidsKg = weight / 1000.0;
        double imc = poidsKg / (tailleM * tailleM);
        imcs.Add(imc);

        int wins = last.Count(x => x == 1);
        int total = last.Count;

        if (countryStats.ContainsKey(countryCode))
        {
            var current = countryStats[countryCode];
            countryStats[countryCode] = (current.wins + wins, current.total + total);
        }
        else
        {
            countryStats[countryCode] = (wins, total);
        }
    }

    var bestCountry = countryStats
        .OrderByDescending(c => c.Value.total == 0 ? 0 : (double)c.Value.wins / c.Value.total)
        .FirstOrDefault();

    string bestCountryCode = bestCountry.Key;
    double bestRatio = bestCountry.Value.total == 0 ? 0 : (double)bestCountry.Value.wins / bestCountry.Value.total;

    double avgImc = imcs.Average();

    var sortedHeights = heights.OrderBy(h => h).ToList();
    double medianHeight;
    int count = sortedHeights.Count;
    if (count % 2 == 0)
        medianHeight = (sortedHeights[count / 2 - 1] + sortedHeights[count / 2]) / 2.0;
    else
        medianHeight = sortedHeights[count / 2];

    doc.Dispose();

    return Results.Json(new
    {
        BestCountry = bestCountryCode,
        BestWinRatio = Math.Round(bestRatio, 2),
        AverageIMC = Math.Round(avgImc, 2),
        MedianHeight = medianHeight
    });
});

app.Run();

public partial class Program { }