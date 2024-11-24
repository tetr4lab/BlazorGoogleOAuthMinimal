using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using WeatherCast.Data;
using ExLibris4.Client.Services;
using Microsoft.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault (args);

// �N���C�A���g�����O�̍Œ჌�x��
builder.Logging.SetMinimumLevel (LogLevel.Information);

// WebAPI��@���ۂɃN�b�L�[��������u���O�t��HTTP�N���C�A���g�v�̐������T�[�r�X��
// ref: https://learn.microsoft.com/ja-jp/aspnet/core/blazor/call-web-api?view=aspnetcore-9.0#cookie-based-request-credentials
builder.Services.AddTransient<CookieHandler> ();
builder.Services.AddHttpClient ("fetch")
    .AddHttpMessageHandler<CookieHandler> ();

// �V�C�\��T�[�r�X
builder.Services.AddScoped<IWeatherForecastServices> (provider => {
    var navigationManager = provider.GetRequiredService<NavigationManager> ();
    var httpClient = provider.GetRequiredService<HttpClient> ();
    var loggerFactory = provider.GetRequiredService<ILoggerFactory> ();
    return new WeatherForecastServices (navigationManager, httpClient, loggerFactory);
});

await builder.Build ().RunAsync ();

/// <summary>�N�b�L�[�������鏈��</summary>
public class CookieHandler : DelegatingHandler {
    /// <summary>���M�����̏㏑��</summary>
    protected override Task<HttpResponseMessage> SendAsync (
        HttpRequestMessage request, CancellationToken cancellationToken) {
        request.SetBrowserRequestCredentials (BrowserRequestCredentials.Include);
        request.Headers.Add ("X-Requested-With", "XMLHttpRequest");
        return base.SendAsync (request, cancellationToken);
    }
}
