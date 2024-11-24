using WeatherCast.Data;

namespace ExLibris4.Services;

/// <summary>規定の天気予報サービス</summary>
public class WeatherForecastServices : IWeatherForecastServices {
    /// <inheritdoc/>
    public async Task<WeatherForecast []?> GetForecastsAsync () => await WeatherForecast.CreateAsync ();
    /// <inheritdoc/>
    public Task<WeatherForecast []?> PostForecastAsync (WeatherForecast forecast) {
        throw new NotImplementedException ();
    }
}
