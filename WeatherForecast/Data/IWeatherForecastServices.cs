namespace WeatherCast.Data;

public interface IWeatherForecastServices {
    public abstract Task<WeatherForecast []?> GetForecastsAsync ();
}
