using WeatherCast.Data;

namespace ExLibris4.Services;

/// <summary>規定の天気予報サービス</summary>
public class WeatherForecastServices : IWeatherForecastServices {
    /// <summary>天気予報一覧を得る</summary>
    /// <returns>一覧</returns>
    public async Task<WeatherForecast []?> GetForecastsAsync () => await WeatherForecast.CreateAsync ();
}
