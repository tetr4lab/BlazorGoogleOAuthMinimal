using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using WeatherCast.Data;

namespace ExLibris4.Client.Services;

/// <summary>規定の天気予報サービス</summary>
public class WeatherForecastServices : IWeatherForecastServices {
    // [Inject]
    private NavigationManager Navigation;
    private HttpClient HttpClient;
    public WeatherForecastServices (NavigationManager navigation, HttpClient httpClient) {
        Navigation = navigation;
        HttpClient = httpClient;
    }

    /// <summary>天気予報一覧を得る</summary>
    /// <returns>一覧</returns>
    public async Task<WeatherForecast []?> GetForecastsAsync ()
        => await HttpClient.GetFromJsonAsync<WeatherForecast []> (Navigation.ToAbsoluteUri ("api/weather/list").ToString ());
}
