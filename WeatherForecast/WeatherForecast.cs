namespace ExLibris4.Weather;

public class WeatherForecast {
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public int TemperatureF => 32 + (int) (TemperatureC / 0.5556);

    public static async Task<WeatherForecast []> Create () {
        var startDate = DateOnly.FromDateTime (DateTime.Now);
        var summaries = new [] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
        var forecasts = Enumerable.Range (1, 5).Select (index => new WeatherForecast {
            Date = startDate.AddDays (index),
            TemperatureC = Random.Shared.Next (-20, 55),
            Summary = summaries [Random.Shared.Next (summaries.Length)]
        }).ToArray ();
        await Task.Delay (500);
        return forecasts;
    }
}
