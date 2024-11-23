namespace WeatherCast.Data;

public class WeatherForecast {

    protected static readonly int MinTemperature = -20;
    protected static readonly int MaxTemperature = 55;
    public DateOnly Date { get; init; }
    public float TemperatureC { get; init; }
    public string Summary => Summaries [Math.Min (Summaries.Length - 1, (int) ((TemperatureC - MinTemperature) / (MaxTemperature - MinTemperature) * Summaries.Length))];
    public float TemperatureF => 32f + (TemperatureC / 0.5556f);
    protected static string [] Summaries { get; } = { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

    /// <summary>本日</summary>
    public WeatherForecast () : this (DateOnly.FromDateTime (DateTime.Now)) { }

    /// <summary>指定数日後</summary>
    /// <param name="days">日数</param>
    public WeatherForecast (int days) : this (DateOnly.FromDateTime (DateTime.Now).AddDays (days)) { }

    /// <summary>指定日</summary>
    public WeatherForecast (DateOnly date) {
        Date = date;
        TemperatureC = Random.Shared.Next (MinTemperature, MaxTemperature);
    }

    /// <summary>指定日1日分 遅延付き</summary>
    public static async Task<WeatherForecast> CreateAsync (DateOnly date) {
        var forecast = new WeatherForecast (date);
        await Task.Delay (100);
        return forecast;
    }

    /// <summary>指定日数後1日分 遅延付き</summary>
    public static async Task<WeatherForecast> CreateAsync (int days) {
        var forecast = new WeatherForecast (days);
        await Task.Delay (100);
        return forecast;
    }

    /// <summary>5日分 遅延付き</summary>
    public static async Task<WeatherForecast []?> CreateAsync () {
        var forecasts = Enumerable.Range (0, 5).Select (index => new WeatherForecast (index)).ToArray ();
        await Task.Delay (500);
        return forecasts;
    }
}
