using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

var builder = WebAssemblyHostBuilder.CreateDefault (args);

// WebAPI��@���ۂɃN�b�L�[��������u���O�t��HTTP�N���C�A���g�v�̐������T�[�r�X��
// ref: https://learn.microsoft.com/ja-jp/aspnet/core/blazor/call-web-api?view=aspnetcore-9.0#cookie-based-request-credentials
builder.Services.AddTransient<CookieHandler> ();
builder.Services.AddHttpClient ("fetch")
    .AddHttpMessageHandler<CookieHandler> ();

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
