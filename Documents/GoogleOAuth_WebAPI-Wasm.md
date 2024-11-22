---
title: Blazor WebAPI - Wasm でGoogle認証を最小規模で使う (ASP.NET Core 8.0)
tags: Blazor .NET OAuth GoogleOAuth2
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
### CORS設定
~~Wasmアプリは、ブラウザに`Same-Origin`と見なされないようなので、`Cross-Origin`を限定的に許容するように設定します。
下記`Host`で取得しているオリジンは、スキーム、ホスト、ポートとも、自身と同一です。
例えば、Wasmページが`https://example.com:5000/test/wasm`であれば、`https://example.com:5000`を渡します。~~

Wasmページでも、同一オリジンと見なされて、設定は不要でした。

### WebAPIの認可
`AuthorizeAttribute`で認可の対象を指定します。

```csharp:BooksController.cs
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Dapper;

[Route ("api/[controller]")]
[ApiController]
public class BooksController : Controller {
    // [Inject]
    protected IDbConnection connection;
    public BooksController (IDbConnection connection) {
        this.connection = connection;
    }

    // 対象に認可を与える
    [Authorize (Policy = "Users")]
    [HttpGet]
    public IActionResult List () {
        return Ok (connection.Query<Book> (@"
select Books.*, Group_concat(AuthorBook.AuthorsId) as _relatedId
from Books
left join AuthorBook on Books.Id = AuthorBook.BooksId
group by Books.Id
limit 100
;"));
    }
}
```

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
@using System.Data
@using System.Net.Http

@page "/books/{Option?}"
@rendermode InteractiveWebAssembly

@inject NavigationManager Navigation
@inject HttpClient HttpClient

<PageTitle>Books</PageTitle>

<h1>Books @(Option)</h1>

<p>running on @(OperatingSystem.IsBrowser() ? "browser" : "server").</p>

@if (books == null || authors == null) {
    <p><em>Loading...</em></p>
} else {
    @foreach (var book in books) {
        <p>
            @if (book != null) {
                <span>Title: @book.Title</span><br />
                <span>Publisher: @book.Publisher</span><br />
                <span>Author: @(string.Join ("、", book.RelatedList.ConvertAll(a => a.Name)))</span><br />
            }
        </p>
    }
}

@code {
    [Parameter] public string? Option { get; set; }

    protected List<Book>? books { get; set; } = null;
    protected List<Author>? authors { get; set; } = null;

    protected override async Task OnInitializedAsync () {
        await base.OnInitializedAsync ();
        if ((Option is null || Option == "oninitialized") && books is null && OperatingSystem.IsBrowser ()) {
            await LoadViaWebApi ();
        }
    }

    protected override async Task OnAfterRenderAsync (bool firstRender) {
        await base.OnAfterRenderAsync (firstRender);
        if (firstRender && Option == "onafterrender" && books is null && OperatingSystem.IsBrowser()) {
            await LoadViaWebApi ();
            StateHasChanged ();
        }
    }

    /// <summary>WebAPIで読み込む</summary>
    protected async Task LoadViaWebApi () {
        books = await HttpClient.GetFromJsonAsync<List<Book>> (Navigation.ToAbsoluteUri ("api/books").ToString ());
        authors = await HttpClient.GetFromJsonAsync<List<Author>> (Navigation.ToAbsoluteUri ("api/authors").ToString ());
        Book.RelatedWholeList = authors;
        Author.RelatedWholeList = books;
    }
}
```

## おわりに
- JWTが必要かと思ったのですが、クッキー認証でできました。
- 初め、CORS設定付けたら上手くいったように誤解したのですが、なくても動きました。
- 最後までお読みいただきありがとうございます。
- 執筆者は、Blazor、ASP.NETともに初学者ですので、誤りもあるかと思います。
    - お気づきの際は、是非コメントや編集リクエストにてご指摘ください。
- あるいは、「それでも解らない」、「自分はこう捉えている」などといった、ご意見、ご感想も歓迎いたします。
