using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/movie", async (string title, IHttpClientFactory httpClientFactory, IConfiguration config) =>
{
    if (string.IsNullOrWhiteSpace(title))
    {
        return Results.BadRequest(new {error = "Введите название фильма"});
    }
    var apiKey = config["OmdbApiKey"];
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        return Results.Problem("API-ключ не настроен");
    }
    var client = httpClientFactory.CreateClient();

    var url = "https://www.omdbapi.com/?apikey=" + Uri.EscapeDataString(apiKey) + "&t=" + Uri.EscapeDataString(title);
    try
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var movie = document.RootElement.Clone();

        if (movie.TryGetProperty("Response", out var result) && result.GetString() == "False")
        {
            return Results.NotFound(new {error = "Фильм не найден" });
        }
        return Results.Json(movie);
    }
    catch (Exception ex)
    {
        return Results.Problem("Не удалось получить данные о фильме")
    }
});

app.Run();