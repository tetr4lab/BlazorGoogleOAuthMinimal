namespace WeatherCast.Data;

public interface IWeatherForecastServices {
    /// <summary>天気予報一覧を得る</summary>
    /// <returns>一覧</returns>
    public abstract Task<WeatherForecast []?> GetForecastsAsync ();
    /// <summary>天気予報を登録する</summary>
    /// <param name="forecast">天気予報</param>
    /// <returns>成否</returns>
    public abstract Task<WeatherForecast []?> PostForecastAsync (WeatherForecast forecast);

}
