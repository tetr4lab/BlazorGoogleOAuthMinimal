---
title: Blazor Web App でGoogle認証を最小規模で使う (ASP.NET Core 8.0)
tags: Blazor .NET OAuth GoogleOAuth2
---

## はじめに
- この記事では、以下のような読者像を想定しています。
    - C#と.NETを囓っている
    - Blazorのチュートリアルを済ませている
    - OAuth認証の概要を理解している。
    - 認証と認可の概念を理解している。
- この記事では、以下のようなプロジェクトを扱います。
    - .NET 8で新たに導入されたBlazor Web Appの「認証なし」テンプレートをベースにします。
    - Google OAuth認証を使用します。
        - `Microsoft.AspNetCore.Authentication.Google`を使います。
        - `Microsoft.AspNetCore.Identity`は使いません。
    - Google ChromeでGoogleアカウントを使うユーザを想定します。
        - ログイン/ログアウトのUIは用意しません。
        - 「ブラウザがログインを認識しているアカウント」を識別します。
    - ポリシーベースの認可を行います。
    - プライベートなオンプレミスサーバでの運用を想定しています。
        - HTTPSを想定しています。(自己署名を含む)
- この記事では、サーバの構成やツール類の導入・使用方法には言及しません。

### 環境
- 開発
    - Windows 11
    - VisualStudio 2022 17.8
    - Microsoft.AspNetCore.Components.WebAssembly 8.0.0
    - Microsoft.AspNetCore.Components.WebAssembly.Server 8.0.0
    - Microsoft.AspNetCore.Authentication.Google 8.0.0
- 実行
    - Debian 12.2.0
    - Microsoft.AspNetCore.App 8.0.0
    - Microsoft.NETCore.App 8.0.0
    - nginx 1.22.1

## プロジェクトの構成
- VisualStudioで新規「Blazor Web App」プロジェクトを以下の構成で作ります。
    - フレームワークは`.NET 8.0`にします。
    - 認証の種類は「なし」にします。
    - デバッグ時のためにHTTPS用の構成にします。
    - `Interactive render mode`は`Auto(Server and WebAssembly)`にします。
    - `Interactivity location`は`Per page/component`にします。
    - ソリューションをプロジェクトと同じディレクトリに配置します。
- NuGetパッケージマネージャーで以下を導入します。
    - `Microsoft.AspNetCore.Authentication.Google`

## Google OAuth 2.0 認証
### 生成
- `Google Cloud Platform` > メニュー > `API とサービス` > `認証情報` > `プロジェクトを選択`プルダウン > `新しいプロジェクト` > `作成`
- `OAuth consent screen` > `外部` > `作成`
    - 必要に応じて、`テストユーザー`に使用者のアカウントを追加します。
- `認証情報` > `+ 認証情報を作成` > `承認済みのリダイレクト URI`
    - `https://localhost:<port>/signin-google`
        - 開発時のポートは`launchSettings.json`に記述があります。
    - `URI を追加` > `https://<server>.<domain>/signin-google`
        - 本番のポートはデフォルト(433)なので指定しません。
    - `URI を追加` > `http://<server>.<domain>/signin-google` > `保存`
- `クライアント ID`と`クライアント シークレット`を取得します。

https://learn.microsoft.com/ja-jp/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-8.0

### シークレット・ストレージに格納
- ストレージの実態は、`%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`の`<key>:<value>`です。
- ストレージは開発用で、パブリッシュには含まれません。

https://learn.microsoft.com/ja-jp/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows#enable-secret-storage

#### ストレージの初期化
- パッケージマネージャコンソール(または開発者用コマンドプロンプト)から以下を実行して、ストレージを初期化します。

```powershell:パッケージマネージャコンソール
PM> dotnet user-secrets init
```
#### ストレージへの格納
- 以下を実行して、`クライアント ID`と`クライアント シークレット`を格納します。

```powershell:パッケージマネージャコンソール
PM> dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>"
PM> dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>"
PM> dotnet user-secrets set "Identity:Claims:EmailAddress:Admin:0" "<admin-mailaddress-0>"
PM> dotnet user-secrets set "Identity:Claims:EmailAddress:User:0" "<user-mailaddress-0>"
PM> dotnet user-secrets set "Identity:Claims:EmailAddress:User:1" "<user-mailaddress-1>"
```

- 後半のメールアドレスは、認可ポリシーに使います。

#### ストレージの参照

- `WebApplicationBuilder.Configuration ["<key>"]`で`<value>`を取得できます。
- 具体的には、サーバ側の[初期化](#初期化)(`Program.cs`)で使用します。

#### ストレージの直接編集
- ソリューション エクスプローラでプロジェクトのコンテキストメニューから「ユーザーシークレットの管理」を選びます。

```json:appsettings.json
    "Authentication:Google:ClientId": "<client-id>",
    "Authentication:Google:ClientSecret": "<client-secret>",
    "Identity:Claims:EmailAddress:Admin:0": "<admin-mailaddress-0>",
    "Identity:Claims:EmailAddress:User:0": "<user-mailaddress-0>",
    "Identity:Claims:EmailAddress:User:1": "<user-mailaddress-1>"
```

- 上記は畳み込まれていますが、以下のようにも書けます。というか、こちらが本来の形式です。

```json:appsettings.json
    "Authentication": {
        "Google": {
            "ClientId": "<client-id>",
            "ClientSecret": "<client-secret>"
        }
    },
    "Identity": {
        "Claims": {
            "EmailAddress": {
                "Admin": [
                    "<admin-mailaddress-0>"
                ],
                "User": [
                    "<user-mailaddress-0>",
                    "<user-mailaddress-1>"
                ]
            }
        }
    },
```

- 配列`<array>: [ "value0", "value1" ]`は、`"<array>:0":"<value0>", "<array>:1":"<value1>"`と等価です。

### ソースコードに格納
- 共有されても支障の無い情報であれば、シークレット・ストレージと同じ形式で、開発・本番とも、`appsettings.json`で保持することが可能です。

### 環境変数に格納
- 本番時に環境変数で保持する場合は、以下のように`:`を`__`に置換します。
- この格納状態は一時的なものです。
    - 永続化する場合は`.bashrc`に設定します。

```bash:bash
$ export Authentication__Google__ClientId='<client-id>'
$ export Authentication__Google__ClientSecret='<client-secret>'
$ export Identity__Claims__EmailAddress__Admin__0='<admin-mailaddress-0>'
$ export Identity__Claims__EmailAddress__User__0='<user-mailaddress-0>'
$ export Identity__Claims__EmailAddress__User__1='<user-mailaddress-1>'
```

- サービス化する場合は`.conf`に設定できます。

```ini:~.conf
[Service]
#~ ~ ~
Environment=Authentication__Google__ClientId='<client-id>'
Environment=Authentication__Google__ClientSecret='<client-secret>'
Environment=Identity__Claims__EmailAddress__Admin__0='<admin-mailaddress-0>'
Environment=Identity__Claims__EmailAddress__User__0='<user-mailaddress-0>'
Environment=Identity__Claims__EmailAddress__User__1='<user-mailaddress-1>'
```

- 以下のようにすることもできます。

```ini:~.conf
[Service]
#~ ~ ~
EnvironmentFile=<path>
```

```bash:<path>
Authentication__Google__ClientId='<client-id>'
Authentication__Google__ClientSecret='<client-secret>'
Identity__Claims__EmailAddress__Admin__0='<admin-mailaddress-0>'
Identity__Claims__EmailAddress__User__0='<user-mailaddress-0>'
Identity__Claims__EmailAddress__User__1='<user-mailaddress-1>'
```

https://learn.microsoft.com/ja-jp/dotnet/core/extensions/configuration-providers#command-line-configuration-provider

## サーバ側アプリの構成
### 起動の構成
- `Program.cs`に以下を加えます。

```csharp:Program.cs
// ~ ~ ~
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
// ~ ~ ~
// クッキーとグーグルの認証を構成
builder.Services.AddAuthentication (options => {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie ()
    .AddGoogle (options => {
        options.ClientId = builder.Configuration ["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration ["Authentication:Google:ClientSecret"]!;
    });

// メールアドレスを保持するクレームを要求する認可用のポリシーを構成
builder.Services.AddAuthorization (options => {
    // 管理者
    options.AddPolicy ("Admin", policyBuilder => {
        policyBuilder.RequireClaim (ClaimTypes.Email, builder.Configuration ["Identity:Claims:EmailAddress:Admin:0"]! );
    });
    // 一般ユーザ (管理者を含む)
    options.AddPolicy ("Users", policyBuilder => {
        policyBuilder.RequireClaim (ClaimTypes.Email, builder.Configuration ["Identity:Claims:EmailAddress:Admin:0"]!, builder.Configuration ["Identity:Claims:EmailAddress:User:0"]!, builder.Configuration ["Identity:Claims:EmailAddress:User:1"]!);
    });
});
// ~ ~ ~
app.UseAuthentication ();
app.UseAuthorization ();
// ~ ~ ~
```

- OAuthのクライアント情報とメンバー情報は、構成マネージャから取得しています。
    - 情報の格納先は構成マネージャによって抽象化されます。

https://learn.microsoft.com/ja-jp/aspnet/core/security/authentication/social/social-without-identity?view=aspnetcore-8.0

### ページの認可
- 全てのページで認証を求めるために、`_Imports.razor`に以下を加えます。
- ページ毎の設定は不要になります。

```razor:_Imports.razor
@using Microsoft.AspNetCore.Authentication
@using Microsoft.AspNetCore.Authentication.Cookies
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@attribute [Authorize (Policy = "Users")]
```

#### 管理者用ページ
- 管理者専用のページでは、管理者用のポリシーを指定します。

```razor:<page>.razor
@page "/<page>"
@attribute [Authorize (Policy = "Admin")]
```

#### 認証不要ページ
- 認証を免除するページには、`AllowAnonymous`属性を加えます。

```razor:<page>.razor
@page "/<page>"
@attribute [AllowAnonymous]
```

https://learn.microsoft.com/ja-jp/aspnet/core/blazor/security/?view=aspnetcore-8.0

https://learn.microsoft.com/ja-jp/aspnet/core/security/authorization/policies?view=aspnetcore-8.0

https://learn.microsoft.com/ja-jp/aspnet/core/security/authorization/simple?view=aspnetcore-8.0

### ポリシーによる選択的表示
- ボディのルートコンポーネントを`CascadingAuthenticationState`で囲むことで、ページに`Task<AuthenticationState>`型のカスケーディングパラメータを渡します。
- これにより、サーバ側ページで次の要素が使用可能になります。
    - コンポーネント`<AuthorizeView>`、`<AuthorizeView Policy="<policy>">`
    - パラメータ`[CascadingParameter] protected Task<AuthenticationState> authState { get; set; };`

```razor:Routes.razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(Program).Assembly" AdditionalAssemblies="new[] { typeof(Client._Imports).Assembly }">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(Layout.MainLayout)" />
            <FocusOnNavigate RouteData="@routeData" Selector="h1" />
        </Found>
        <NotFound>
            <LayoutView Layout="@typeof(Layout.MainLayout)">
                <p>Sorry, there's nothing at this address.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

### 未認証時のページ
- 認証されていない場合にデフォルトでリダイレクトされる`/Account/AccessDenied`を用意します。
- ここでは、認証と認可の状態に応じて表示を切り替えています。
- ユーザ名を表示するだけのためにカスケーディングパラメータを受け取っています。

```razor:AccessDenied.razor
@page "/Account/AccessDenied"
@attribute [AllowAnonymous]

<PageTitle>AccessDenied</PageTitle>

<AuthorizeView>
    <Authorized><h1>@(identity?.Name ?? "World")さん、こんにちは</h1></Authorized>
    <NotAuthorized><h1>Googleアカウントに<a href="https://accounts.google.com/ServiceLogin" target="_blank">ログイン</a>してください。</h1></NotAuthorized>
</AuthorizeView>
<AuthorizeView Policy="Users">
    <Authorized><p>あなたは、承認されたユーザです。</p></Authorized>
    <NotAuthorized><p>残念ながら、あなたは、承認されていません。</p></NotAuthorized>
</AuthorizeView>
<AuthorizeView Policy="Admin">
    <Authorized><p>あなたは、管理者です。</p></Authorized>
</AuthorizeView>

@code {
    [CascadingParameter] protected Task<AuthenticationState> AuthState { get; set; } = null!;
    private System.Security.Principal.IIdentity? identity = null;
    protected override async Task OnInitializedAsync () => identity = (AuthState != null) ? (await AuthState).User?.Identity : null;
}
```

### ホームページ
- 管理者の識別と認証情報を表示します。

```razor:Home.razor
@page "/"

<PageTitle>Home</PageTitle>

<h1>Hello, @(identity?.Name ?? "World")</h1>
<p>Welcome to your new app.</p>
<DumpIdentity Identity="identity" />
<AuthorizeView Policy="Admin">
    <Authorized><p>あなたは、管理者です。</p></Authorized>
    <NotAuthorized><p>あなたは、一般ユーザです。</p></NotAuthorized>
</AuthorizeView>

@code {
    [CascadingParameter] protected Task<AuthenticationState> authState { get; set; } = null!;
    private System.Security.Claims.ClaimsIdentity? identity = null;
    protected override async Task OnInitializedAsync () => identity = (authState != null) ? (await authState).User?.Identity as System.Security.Claims.ClaimsIdentity : null;
}
```

- 以下は、認証情報を表示するコンポーネントです。

```razor:DumpIdentity.razor
@namespace ExLibris4.Components

<p>
    @if (Identity != null) {
        @($"Name: {Identity.Name}")<br />
        @($"IsAuthenticated: {Identity.IsAuthenticated}")<br />
        @($"Type: {Identity.GetType()}")<br />
        @($"BootstrapContext: {Identity.BootstrapContext}")<br />
        @($"Actor: {Identity.Actor}")<br />
        @($"Label: {Identity.Label}")<br />
        @($"AuthenticationType: {Identity.AuthenticationType}")<br />
        @($"NameClaimType: {Identity.NameClaimType}")<br />
        @($"RoleClaimType: {Identity.RoleClaimType}")<br />
        @($"Claims: [{string.Join(", ", Identity.Claims.ToList().ConvertAll(c => $@"{{""Type"":""{ c.Type}"", ""Value"":""{c.Value}"", ""ValueType"":""{c.ValueType}""}}"))}]")<br />
    } else {
        <span>no identity</span>
    }
</p>

@code {
    [Parameter]
    public System.Security.Claims.ClaimsIdentity? Identity { get; set; } = null;
}
```

## クライアント側の認可
- 一応、クライアント側でもサーバ同様にしておきます。

```razor:<project>.Client/_Imports.razor
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize (Policy = "Users")]
```

## 留意点
- ブラウザでアカウントからログアウトしても、たとえアカウントを切り替えても、元のセッションが有効な間は最初のアカウントでログインしたままになります。
- いったんブラウザを閉じて開き直すと、リセットされます。

## おわりに
- 執筆者は、Blazor、ASP.NETともに初学者ですので、誤りもあるかと思います。
    - お気づきの際は、是非コメントや編集リクエストにてご指摘ください。
- あるいは、「それでも解らない」、「自分はこう捉えている」などといった、ご意見、ご感想も歓迎いたします。
