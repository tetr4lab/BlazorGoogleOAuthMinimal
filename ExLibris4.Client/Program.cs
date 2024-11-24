using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using WeatherCast.Data;
using ExLibris4.Client.Services;
using Microsoft.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault (args);

// クライアント側ログの最低レベル
builder.Logging.SetMinimumLevel (LogLevel.Information);

// WebAPIを叩く際にクッキーを加える「名前付きHTTPクライアント」の生成をサービス化
// ref: https://learn.microsoft.com/ja-jp/aspnet/core/blazor/call-web-api?view=aspnetcore-9.0#cookie-based-request-credentials
builder.Services.AddTransient<CookieHandler> ();
builder.Services.AddHttpClient ("fetch")
    .AddHttpMessageHandler<CookieHandler> ();

// 天気予報サービス
builder.Services.AddScoped<IWeatherForecastServices> (provider => {
    var navigationManager = provider.GetRequiredService<NavigationManager> ();
    var httpClient = provider.GetRequiredService<HttpClient> ();
    var loggerFactory = provider.GetRequiredService<ILoggerFactory> ();
    return new WeatherForecastServices (navigationManager, httpClient, loggerFactory);
});

await builder.Build ().RunAsync ();

/// <summary>クッキーを加える処理</summary>
public class CookieHandler : DelegatingHandler {
    /// <summary>送信処理の上書き</summary>
    protected override Task<HttpResponseMessage> SendAsync (
        HttpRequestMessage request, CancellationToken cancellationToken) {
        request.SetBrowserRequestCredentials (BrowserRequestCredentials.Include);
        request.Headers.Add ("X-Requested-With", "XMLHttpRequest");
        return base.SendAsync (request, cancellationToken);
    }
}
