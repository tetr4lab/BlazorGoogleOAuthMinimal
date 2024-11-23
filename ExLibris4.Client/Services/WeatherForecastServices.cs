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
    public async Task<WeatherForecast []?> GetForecastsAsync ()
        => await HttpClient.GetFromJsonAsync<WeatherForecast []> (Navigation.ToAbsoluteUri ("api/weather").ToString ());

    /// <inheritdoc/>
    public async Task<bool> PostForecastAsync (WeatherForecast forecast) {
        var logger = LoggerFactory.CreateLogger<ClientHome> ();
        using var responce = await HttpClient.PostAsJsonAsync (Navigation.ToAbsoluteUri ("api/weather").ToString (), forecast);
        logger.LogInformation ($"PostForecastAsync {responce.StatusCode} {responce.ReasonPhrase} {responce.TrailingHeaders}");
        return responce.IsSuccessStatusCode;
    }

}
