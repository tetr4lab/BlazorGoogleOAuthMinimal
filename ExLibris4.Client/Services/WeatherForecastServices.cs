using ExLibris4.Client.Pages;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using WeatherCast.Data;

namespace ExLibris4.Client.Services;

/// <summary>規定の天気予報サービス</summary>
public class WeatherForecastServices : IWeatherForecastServices {
    // [Inject]
    private NavigationManager Navigation;
    private HttpClient HttpClient;
    private ILoggerFactory LoggerFactory;
    public WeatherForecastServices (NavigationManager navigation, HttpClient httpClient, ILoggerFactory loggerFactory) {
        Navigation = navigation;
        HttpClient = httpClient;
        LoggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    public async Task<WeatherForecast []?> GetForecastsAsync () {
        var logger = LoggerFactory.CreateLogger<ClientHome> ();
        try {
            return await HttpClient.GetFromJsonAsync<WeatherForecast []> (Navigation.ToAbsoluteUri ("api/weather").ToString ());
        }
        catch (Exception ex) {
            logger.LogInformation ($"GetForecastsAsync {ex}");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<WeatherForecast []?> PostForecastAsync (WeatherForecast forecast) {
        var logger = LoggerFactory.CreateLogger<ClientHome> ();
        try {
            using var responce = await HttpClient.PostAsJsonAsync (Navigation.ToAbsoluteUri ("api/weather").ToString (), forecast);
            var posted = await responce.Content.ReadFromJsonAsync<WeatherForecast> ();
            logger.LogInformation ($"PostForecastAsync {responce.StatusCode} {responce.ReasonPhrase} {posted}");
            return posted is null ? null : [posted];
        }
        catch (Exception ex) {
            logger.LogInformation ($"PostForecastAsync {ex}");
            return null;
        }
    }

}
