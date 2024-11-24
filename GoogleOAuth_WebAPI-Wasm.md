---
title: Blazor WebAPI - Wasm でGoogle認証を最小規模で使う (ASP.NET Core 8.0)
tags: Blazor AspNetCore OAuth GoogleOAuth2 Wasm
---

## はじめに
この記事は、以下の記事の続編です。

https://zenn.dev/link/articles/1946ec08aec508

前記事では、サーバ側もクライアント側も、ブラウザでページにアクセスする際の認証を扱いました。  
当記事では、クライアント側のアプリ(Wasm)からサーバ側のWebAPIにアクセスする際の認証を扱います。

### 概要
WebAPIに認可を設定します。
Wasmページからのリクエストにクッキーを付加します。

### 環境
以下の環境で検証しました。

https://zenn.dev/link/articles/ad947ade600764

## サーバ側の構成
### WebAPIの認可
`AuthorizeAttribute`で認可の対象を指定します。
認可は、アクションに対して付与することも、コントローラ全体に対して付与することも可能です。

```csharp:WeatherController.cs
[Route ("api/[controller]/[action]")]
[ApiController]
public class WeatherController : Controller {
    // 対象に認可を与える
    [Authorize (Policy = "Users")]
    [HttpGet]
    public async Task<IActionResult> List () {
        return Ok (await WeatherForecast.CreateAsync ());
    }
}
```

https://learn.microsoft.com/ja-jp/aspnet/core/web-api/?view=aspnetcore-8.0

https://learn.microsoft.com/ja-jp/aspnet/core/security/authorization/simple?view=aspnetcore-8.0

https://learn.microsoft.com/ja-jp/dotnet/api/microsoft.aspnetcore.authorization.authorizeattribute?view=aspnetcore-8.0

## クライアント側の構成
### クッキー付き HttpClient
WebAPIに`HttpClient`でアクセスする際にヘッダにクッキーを付加するようにします。

```csharp:Program.cs
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

var builder = WebAssemblyHostBuilder.CreateDefault (args);

// WebAPIを叩く際にクッキーを加える「名前付きHTTPクライアント」の生成をサービス化
builder.Services.AddTransient<CookieHandler> ();
builder.Services.AddHttpClient ("fetch")
    .AddHttpMessageHandler<CookieHandler> ();

await builder.Build ().RunAsync ();
```

```csharp:CookieHandler.cs
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
```

https://learn.microsoft.com/ja-jp/aspnet/core/blazor/call-web-api?view=aspnetcore-9.0#cookie-based-request-credentials

### WebAPI から取得
認証を使わない場合と変わりません。

```razor:BookList.cs
    /// <summary>天気予報一覧を得る</summary>
    /// <returns>一覧</returns>
    public async Task<WeatherForecast []?> GetForecastsAsync ()
        => await HttpClient.GetFromJsonAsync<WeatherForecast []> (Navigation.ToAbsoluteUri ("api/weather/list").ToString ());
```

## おわりに
- JWTが必要かと思ったのですが、クッキー認証でできました。
- 初め、CORS設定付けたら上手くいったように誤解したのですが、なくても動きました。
- 最後までお読みいただきありがとうございます。
- 執筆者は、Blazor、ASP.NETともに初学者ですので、誤りもあるかと思います。
    - お気づきの際は、是非コメントや編集リクエストにてご指摘ください。
- あるいは、「それでも解らない」、「自分はこう捉えている」などといった、ご意見、ご感想も歓迎いたします。
