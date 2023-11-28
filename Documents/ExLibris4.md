---
title: Blazor Web App in ASP.NET 8.0  (4)
tags: Blazor MudBlazor .NET MariaDB
---
# Blazor in ASP.NET 8.0 (4)

## はじめに
- この記事では、シンプルなBlazor Web Appを作ってみます。
    - MySqlConnectorとDapperを使ってMariaDBのデータを扱います。
        - EF Coreは使いません。
    - Microsoft.AspNetCore.Authentication.Googleを使って認証を行います。
        - Microsoft.AspNetCore.Identityは使いません。
- この記事では、以下のような読者像を想定しています。
    - C#と.NETを囓っている
    - 一般的なMVCを知っている
    - データベースのスキーマとSQLになじみがある
    - Blazorのチュートリアルを済ませた
- この記事ではツール類の使用方法には言及しません。
- 「(1)~(3)はどこ?」 ⇒ ごめんなさい、公開されていません。

### 環境
- Windows 11
- VisualStudio 2022 17.8
- .NET 8.0
- MySqlConnector 2.3.1
- dapper 2.1.24
- MudBlazor 6.11.1
- MySql 8.0.35 ubuntu
- MariaDB 15.1 debian

### 題材
- 書籍と著者のテーブルを閲覧、編集、追加、削除可能なアプリを作ります。
- 書籍と著者は多対多の関係で、中間テーブルが使われます。

### ホスティングモデル
- オンプレミスな ASP.NET Core サーバを使います。

[Windows から Blazor Web App をデプロイする Debian Server の構成]() (Qiita)

### 非対応事項
#### 排他制御
- このプロジェクトでは排他制御を行いません。
- 代わりといっては何ですが、リアルタイムにセッション数を表示して、競合の可能性を示します。

#### アカウント管理と認証
- このプロジェクトではアカウント管理や認証を行いません。
- 単なる実証実験、あるいは、ローカルネットワーク内で、1人で使うことを想定しています。

## プロジェクトの構成
- VisualStudioで新規「Blazor Web App」プロジェクトを以下の想定で作ります。
    - フレームワークは`.NET 8.0`にします。
    - 認証の種類は「なし」にします。
        - 「個別のアカウント」にすると、ローカルSQLサーバーが導入されます。
    - HTTPS用の構成にします。
    - `Interactive render mode`は`Auto(Server and WebAssembly)`にします。
    - `Interactivity location`は`Per page/component`にします。
    - プロジェクト名を`ExLibris4`とします。
    - ソリューションをプロジェクトと同じディレクトリに配置します。
- NuGetパッケージマネージャーで以下を導入します。
    - 導入対象
        - `Microsoft.AspNetCore.Authentication.Google`
        - `MySqlConnector`
        - `Dapper`
    - コンソールを使う場合は`dotnet add package 《パッケージ名》`とします。

## SNS認証 (Google OAuth)
### 準備
#### トークン生成
- `Google Cloud Platform` > メニュー > `API とサービス` > `認証情報` > `プロジェクトを選択`プルダウン > `新しいプロジェクト` > `作成`
- `OAuth consent screen` > `外部` > `作成`
    - `テストユーザー`に使用者のアカウントを追加します。
- `認証情報` > `+ 認証情報を作成` > `承認済みのリダイレクト URI` > `https://localhost:<port>/signin-google` > `保存`
    - `<port>`は`launchSettings.json`に記述があります。
    - パブリッシュ後のポートは`http`が`5000`、`https`が`5001`です。
    - `クライアント ID`と`クライアント シークレット`を取得します。

https://learn.microsoft.com/ja-jp/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-8.0

#### クライアント ID とシークレットをシークレット ストレージに格納

```powershell
PM> dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>"
PM> dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>"
```

- ストレージの初期化が必要な場合があります。

```powershell
PM> dotnet user-secrets init
```

https://learn.microsoft.com/ja-jp/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows#enable-secret-storage

### 導入
- NuGetパッケージマネージャーで以下を導入します。
    - 導入対象
        - `Microsoft.AspNetCore.Authentication.Google`

### 構成
- `Program.cs`に以下を加えます。

```csharp:Program.cs
// ~ ~ ~
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
// ~ ~ ~
builder.Services.AddAuthentication (options => {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie ()
    .AddGoogle (googleOptions => {
        googleOptions.ClientId = builder.Configuration ["Authentication:Google:ClientId"]!;
        googleOptions.ClientSecret = builder.Configuration ["Authentication:Google:ClientSecret"]!;
    });
// ~ ~ ~
app.UseAuthentication ();
app.UseAuthorization ();
// ~ ~ ~
```

- `_Imports.razor`に以下を加えます。

```razor:_Imports.razor
@using Microsoft.AspNetCore.Authentication
@using Microsoft.AspNetCore.Authorization
```

- 認可対象の`<page>.razor`に`Authorize`属性を加えます。

```razotor
@page "/<page>"
@attribute [Authorize]
```

https://learn.microsoft.com/ja-jp/aspnet/core/security/authentication/social/social-without-identity?view=aspnetcore-8.0

### 検証
- ログアウトを実装しない場合に簡易な検証方法として、GCPコンソールでリダイレクトURIから対応ポートを削除する方法が考えられます。


## UIフレームワーク (MudBlazor)
### 概要
- MudBlazorは、数あるBlazor用のUIフレームワークの一つです。
- C#でコントロールしやすいように作られていて、.NETプログラマには使いやすいです。

### 導入
- 公式サイトのインストールページを確認し、既存のプロジェクトに導入する場合は「Manual Install」に従って作業します。
    - [Installation](https://mudblazor.com/getting-started/installation#online-playground)
        - このプロジェクトでは、手動インストールを採用しました。
    - 新規プロジェクトを作成する場合は「Using our dotnet templates」に従うと簡易です。

- まず、NuGetパッケージを導入します。

```text:NuGet Package Manager Console
PM> dotnet add package MudBlazor
```
- コンソールを使うと、導入中に文字化けが生じる場合は、IDEとコンソールで文字コードが異なる(utf8:sjis)可能性があります。
    - `PM> intl.cpl` → 「管理」タブ → 「システムロケールの変更…」 → 「ベータ: ワールドワイド言語サポートで Unicode UTF-8を使用」にチェックすることで、以後、化けなくなりました。
- 導入が済んだら、`_Imports.razor`に名前空間を加えます。

```razor:_Imports.razor
@using MudBlazor
```

- `Pages/_Host.cshtml`のヘッダにフォントとスタイルシートへの参照を加えます。

```cshtml:Pages/_Host.cshtml
<link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
```

- `Pages/_Host.cshtml`のボディにスクリプトへの参照を加えます。

```cshtml:Pages/_Host.cshtml
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```

- `wwwroot/css/`から`bootstrap`と`open-iconic`を削除します。
    - `Pages/_Host.cshtml`のヘッダからも参照を除去します。
- 同様に`site.css`も不要です。
    - なお、このプロジェクトでは、中身を消去した上で書き換えて使用するので、この参照は残しました。

```cshtml:Pages/_Host.cshtml
<link rel="stylesheet" href="css/bootstrap/bootstrap.min.css" />
<link href="css/site.css" rel="stylesheet" />
```

- `Program.cs`でサービスを登録します。

```csharp:Program.cs
using MudBlazor.Services;
// ～
builder.Services.AddMudServices();
```

- `Shared/MainLayout.razor`にコンポーネントを導入します。

```razor:Shared/MainLayout.razor
<MudThemeProvider/>
<MudDialogProvider/>
<MudSnackbarProvider/>
```

### 起動時オプションの構成
- 起動時オプションを追加します。
    - このプロジェクトでは、スナックバーのデフォルトを設定しています。

```csharp:Program.cs
builder.Services.AddMudServices (config => {
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});
```

## データベースの構成
### データベースの基礎設計
- 書籍テーブル
    - ID、タイトル、出版(発売)日、価格、出版社、叢書、詳細、著者(複数)
- 著者テーブル
    - ID、名前、補助名、詳細、書籍(複数)
- 実際のデータベース設計であれば、「著者(複数)」と「書籍(複数)」ではなく、「書籍-著書中間テーブル」を用意することになりますが、その辺りは、EntityFramework Coreにお任せです。

### テーブルの一般化
- 二つのテーブルを共通に扱えるように一般化します。
    - これにより、コンポーネントのコードを共有可能にします。
- テーブル
    - 疑似列: テーブル名、行名、列名、検索対象、関連リスト名、関連リスト
    - 機能: 複製、更新
- テーブル名と列名は全ての行で共通で、他は行毎に個別です。

### インターフェイスの作成
- 一般化した設計に合わせて、モデルのベースとなるインターフェイス(ExLibris/Data/IExLibrisModel.cs)を書きます。

```csharp:ExLibris/Data/IExLibrisModel.cs
```

- `IExLibrisModel<T>`は、テーブル群に共通の主疑似列と機能を規定します。
- `IExLibrisRelatedModel<T>`は、関係先テーブルに関する疑似列を規定します。
- これらが(`<T1, T2>`とまとめずに)二つに分かれているのは、二つの型引数をrazorに導入する際に生じる循環参照を回避するためです。
    - C#で書く分には問題ないのですが、razorでは`@typeparam`2行になるとエラーが生じます。
- ゲッターとセッターの両方を持つインスタンスプロパティは列と見なさるので、`[NotMapped]`を付けて回避します。
    - ここに付けても作用はありませんが、継承先で付ける必要性を示すものです。

### モデルの作成
#### エンティティクラスの作成
- 主要なテーブルの分だけエンティティクラスを作ります。
    - 先ほど用意したインターフェイスを継承したクラスとして定義します。
    - 主テーブルだけを明示的に定義し、中間テーブルは(EF Coreによって)勝手に作られることを期待して用意しません。
- 以下は、書籍(ExLibris/Data/Book.cs)と著者(ExLibris/Data/Author.cs)です。

```csharp:ExLibris/Data/Book.cs
```

```csharp:ExLibris/Data/Author.cs
```

#### データベースコンテキストの作成
- エンティティクラスをDBに接続します。
- 以下は、コンテキスト(ExLibris/Data/ExLibrisContext.cs)です。

```csharp:ExLibris/Data/ExLibrisContext.cs
using Microsoft.EntityFrameworkCore;
namespace ExLibris.Data; 
public class ExLibrisContext : DbContext {
    public ExLibrisContext (DbContextOptions options) : base (options) { }
    public DbSet<Book> Books { get; set; }
    public DbSet<Author> Authors { get; set; }
}
```

- `DbContext`を継承した上で、`DbContextOptions`を注入し、`DbSet<T>`のプロパティとしてテーブルを定義しています。

### データベースの接続名
- `appsettings.json`にデータベースファイルのパスと接続名を追加します。

```json:ExLibris/appsettings.json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning"
        }
    },
    "ConnectionStrings": {
        "ExLibris": "server=localhost;user=<username>;password=<password>;database=exlibris"
    },
    "AllowedHosts": "*"
}
```

- この文字列は、以下のコードで取得されます。

```csharp
var builder = WebApplication.CreateBuilder (args);
var connectionString = builder.Configuration.GetConnectionString ("ExLibris");
var serverVersion = new MySqlServerVersion (new Version (10, 11, 4));
```

- この機構によって、データベースの接続名が、ビルドし直すことなく変更可能になります。

https://learn.microsoft.com/ja-jp/ef/core/miscellaneous/connection-strings

### 起動の構成
- `Program.cs`にデータベースの接続サービスを追加します。

```csharp:Program.cs
// Add services to the container.
builder.Services.AddRazorPages ();
builder.Services.AddServerSideBlazor ();
builder.Services.AddDbContext<ExLibrisContext> (
	dbContextOptions => dbContextOptions
		.UseMySql (connectionString, serverVersion)
		// 以下3行はデバッグ用
		.LogTo (Console.WriteLine, LogLevel.Information)
		.EnableSensitiveDataLogging ()
		.EnableDetailedErrors ()
);
```

https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql#2-services-configuration

### マイグレーション
#### マイグレーションファイルの生成
- Entity Framework Coreにモデルなどを解析させてデータベースを構築できるようにします。

```text:NuGet Package Manager Console
PM> dotnet ef migrations add InitialCreate
Build started...
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
PM> 
```

- 実行すると、`Migrations`フォルダとファイルが作られます。

#### マイグレーションの実施
- マイグレーションを実施してデータベースファイルを生成します。

```text:NuGet Package Manager Console
PM> dotnet ef database update
Build started...
Build succeeded.
info: ~
~
~
Done.
PM> 
```

- 無事にデータベースファイルが生成されました。
    - プロパティのアトリビュートなど、細かな指定はしませんでしたが、特に問題ないようです。
    - 中間テーブルも無事にできています。

```sql:ExLibris.db
CREATE TABLE "Books" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Books" PRIMARY KEY AUTOINCREMENT,
    "Title" TEXT NULL,
    "PublishDate" TEXT NULL,
    "Publisher" TEXT NULL,
    "Series" TEXT NULL,
    "Price" TEXT NOT NULL,
    "Description" TEXT NULL
);
CREATE TABLE "Authors" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Authors" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NULL,
    "AdditionalName" TEXT NULL,
    "Description" TEXT NULL
);
CREATE TABLE "AuthorBook" (
    "AuthorsId" INTEGER NOT NULL,
    "BooksId" INTEGER NOT NULL,
    CONSTRAINT "PK_AuthorBook" PRIMARY KEY ("AuthorsId", "BooksId"),
    CONSTRAINT "FK_AuthorBook_Authors_AuthorsId" FOREIGN KEY ("AuthorsId") REFERENCES "Authors" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AuthorBook_Books_BooksId" FOREIGN KEY ("BooksId") REFERENCES "Books" ("Id") ON DELETE CASCADE
);
```

#### 追加の構成

- モデルなどに変更が必要になった際は、コードの変更後に以下でマイグレーションフォルダにファイルを生成します。
    - `dotnet ef migrations add 《マイグレーションの名前》`
- マイグレーションの実施は、以下を使います。
    - `dotnet ef database update`
- ロールバックが必要になった場合は、以下のようにします。
    - `dotnet ef database update 《マイグレーションファイル名》`
- マイグレーション適用前に、追加生成ファイルを削除する場合は、以下のようにします。
    - `dotnet ef migrations remove`
- 他にも色々あります。
    - [Entity Framework Core Tool CLI/ASP.NET Core Env.](https://learn.microsoft.com/ja-jp/ef/core/cli/dotnet#aspnet-core-environment)

#### テストデータの導入
- [こちらの外部サイト](http://complex.matrix.jp/comics/search.cgi)から、1ヶ月分のコミックスの発売日情報をタブ区切りテキストとして取得しました。
    - 成人向けの書名も含まれますのでご注意ください。
- `ExLibris.db`を`DB Browser`で開き、取得したtsv(.csv)を新規`Published`テーブルとしてインポートします。
- 取り込み後、以下のようにカラムを設定しました。

```sql:Publishedのスキーマ
CREATE TABLE "Published" (
	"Title"	TEXT,
	"Author"	TEXT,
	"PublishDate"	TEXT,
	"Publisher"	TEXT,
	"Series"	TEXT,
	"Price"	INTEGER,
	"Id"	INTEGER
)
```

- 日付の形式がドット区切りでしたので、SQLite風に書き換えます。

```sql
UPDATE Published SET PublishDate = replace (PublishDate, ".", "-");
```

- 書籍のテーブルに、AuthorとId以外の情報を引き写します。

```sql
INSERT INTO Books (Title, PublishDate, Publisher, Series, Price)
	SELECT Title, PublishDate, Publisher, Series, Price
	FROM Published
	;
```

- 著者名のフィールドには、`/`で区切られて複数の著者名が並んでいます。
    - 個別の著者名に分割してjson配列を生成し、さらにそれを行に分解します。
        - 列名は`value`になります。
        - 行は昇順で並べ替えます。
    - 重複を省きます。
    - `Authors.Name`に挿入します。

```sql
INSERT INTO Authors (Name)
    SELECT DISTINCT value 
        FROM Published 
        JOIN json_each('["' || replace (Author, '/', '","') || '"]') 
        ORDER BY value
    ;
```

- `Published.Author`に含まれる`Authors.Name`の`Authors.Id`と、`Published.Title`と一致する`Books.Title`の`Books.Id`をセットにして、連結テーブル(`AuthorBook`)に挿入します。

```sql
INSERT INTO AuthorBook (AuthorsId, BooksId)
	SELECT Authors.Id as AuthorsId, Books.Id as BooksId
		FROM Published
			JOIN Books USING (Title, Publisher, PublishDate)
			JOIN Authors ON '/'||Published.Author||'/' like '%/'||Authors.Name||'/%'
	;
```

- 最後にインポートしたテーブルを削除(`DROP TABLE Published;`)しておきます。
- 正しく導入できていれば、以下で、タイトルと著者が一覧されます。

```sql
SELECT Books.Title, group_concat(Authors.Name) as "Author(s)"
	FROM Books
	JOIN AuthorBook ON Books.Id == BooksId
	JOIN Authors ON AuthorsId == Authors.Id
	GROUP BY Books.Id
	;
```

## サービスとしてDB入出力を構成する
### DB入出力サービスの構成
- データベースとのやりとりを集約したサービスを構成します。
- このプロジェクトでは、WebAPIは使わず、メソッドを直接呼び出します。
- 機能として、一覧、追加、削除、反映を用意します。

#### サービスの作成
- このサービスに、DBとの入出力を集約します。
- 型引数でモデルに振り分けます。

```csharp:Services/ExLibrisDataSet.cs
```

#### サービスの登録
- Program.csでスコープ付きサービスとして登録します。

```csharp:Program.cs
builder.Services.AddDbContext<ExLibrisContext> (options => options.UseSqlite (connectionString));
builder.Services.AddScoped<ExLibrisDataSet> ();
```

#### `DbContext`はスレッドセーフでない
- この実装は、以下のリンク先の制約に留意する必要があります。

https://learn.microsoft.com/ja-jp/aspnet/core/blazor/blazor-ef-core?view=aspnetcore-7.0#database-access

- 要約すると…
    - `DbContext`はスレッドセーフではありません。
    - シングルトンはもちろん、スコープで管理しても、不適切な同時使用が生じる可能性があります。
    - 操作毎に生成し、フラグを用いて再入を抑止しましょう。

##### 依存性の注入を使わずに、new で単純に生成する
- この方法はシンプルで良いのですが、`ConnectionString`の参照が分散してしまいます。

https://learn.microsoft.com/ja-jp/ef/core/dbcontext-configuration/#simple-dbcontext-initialization-with-new

##### DbContext の代わりに、DbContext ファクトリを注入する
- このプロジェクトでは、こちらの方法を採用しています。

https://learn.microsoft.com/ja-jp/ef/core/dbcontext-configuration/#using-a-dbcontext-factory-eg-for-blazor

#### 変更の追跡と値の反映
##### [クエリを実行し、変更を適用する](https://learn.microsoft.com/ja-jp/ef/core/change-tracking/identity-resolution#query-then-apply-changes)
- クエリで取得したエンティティを更新して、変更を保存すると反映されます。

```csharp
public async Task UpdateAsync<T> (T item) where T : class {
    using (var context = _contextFactory.CreateDbContext ()) {
        if (item is Book book) {
            var entity = await context.Books.FindAsync (book.Id);
            if (entity != null) {
                context.Entry (entity).CurrentValues.SetValues (book);
            }
        } else if (item is Author author) {
            // ~ ~ ~
        }
        await context.SaveChangesAsync ();
    }
}
```

- 最初、値のコピーを自前でやろうとしたのですが、トラッカーを混乱させてしまうようで、例外が生じて上手くいきませんでした。

```csharp
if (entity != null) {
    book.CopyTo (entity);
```

```
System.InvalidOperationException: 'The instance of entity type 'Book' cannot be tracked because another instance with the key value '{Id: 1347}' is already being tracked. When attaching existing entities, ensure that only one entity instance with a given key value is attached.'
```

##### [Update を呼び出す](https://learn.microsoft.com/ja-jp/ef/core/change-tracking/identity-resolution#call-update)
- こちらの方法は、先のものよりシンプルで良さそうに思えたのですが、保存(`SaveChanges`)中に例外が生じて上手くいかないようです。
- なぜか、中間テーブルが重複して生成されてしまうようです。

```csharp
public async Task UpdateAsync<T> (T item) where T : class {
    using (var context = _contextFactory.CreateDbContext ()) {
        if (item is Book book) {
            context.Update (book);
        } else if (item is Author author) {
```

```
Microsoft.EntityFrameworkCore.DbUpdateException: 'An error occurred while saving the entity changes. See the inner exception for details.'
~
MySqlException: Duplicate entry '1829-1347' for key 'AuthorBook.PRIMARY'
```

##### [元の値を使用する](https://learn.microsoft.com/ja-jp/ef/core/change-tracking/identity-resolution#use-original-values)
- 対して、こちらの「差分を検出する」方法は、正常に機能するようです。

```csharp
public async Task UpdateAsync<T> (T currentItem, T originalItem) where T : class {
    using (var context = _contextFactory.CreateDbContext ()) {
        if (currentItem is Book book && originalItem is Book) {
            context.Attach (book);
            context.Entry (book).OriginalValues.SetValues (originalItem);
        } else if (currentItem is Author author && originalItem is Author) {
```

##### クエリを実行して競合を検出する
- 元の値を保持しているなら、クエリを実行して同一性を検証することで、競合を検出することが可能です。
- 同一性の検証では、対象項目の参照でなく値の検証が必要で、さらには、`ForeignKey`の関係先のIDも比較しなければなりません。
- `entity`を得るのに`FindAsync (book.Id)`を使う場合は、正しく`ForeignKey`を得るために、関係データを[明示的に読み込む](https://learn.microsoft.com/ja-jp/ef/core/querying/related-data/explicit)必要があります。

```csharp
public async Task<bool> UpdateAsync<T> (T current, T original) where T : class {
    var updated = false;
    using (var context = _contextFactory.CreateDbContext ()) {
        if (current is Book book && original is Book) {
            var entity = await context.Books.FindAsync (book.Id);
            if (entity != null) {
                await context.Entry (entity).Collection (o => o.Authors).LoadAsync ();
                if (entity.Equals (original)) {
                    context.Entry (entity).CurrentValues.SetValues (book);
                    updated = true;
                }
            }
        } else if (current is Author author && original is Author originalAuthor) {
```

- あるいは、`Include`を使って[一括読み込み](https://learn.microsoft.com/ja-jp/ef/core/querying/related-data/eager)を使うこともできます。

```csharp
            var entity = await context.Books.Include (o => o.Authors).Where (o => o.Id == book.Id).FirstAsync ();
            if (entity != null && entity.Equals (original)) {
                context.Entry (entity).CurrentValues.SetValues (book);
                updated = true;
            }
```

https://learn.microsoft.com/ja-jp/ef/core/querying/related-data/

- なお、上の例では、競合が生じたときに上書きを抑制するだけで、解決を行っておらず、後から更新しようとした内容は失われます。

## ホームとナビゲーションバー
- 全てのページトップに固定された横並びメニューバーを置きます。
    - バーの背後にページコンテンツが隠れないように、ページコンテンツ上部をパディングします。
- 横幅が狭いときはバーを非表示にして、ドロワーを開くボタンと開いたドロワーを表示します。
    - ボタンとドロワーはページコンテンツを背後に隠します。
- バーの付属物として、検索フィールドとボタン、現在のページの見出し、セッション数を表示します。

### レイアウト
- レイアウト(`Shared/MainLayout.razor`)に記述することで、全ページで共有します。
    - ナビゲーションバーはレイアウトに直接記述せず、コンポーネントにします。
- ナビゲーションバーの検索文字列、ボタンの押下、ページの見出しは、レイアウトで保持して、ページとの間で共有します。
- ナビゲーションバーには、検索文字列を更新するコールバック先をパラメータで渡します。
- ページには、ページの見出しを更新するコールバック先と検索文字列を渡します。
    - ページへ`@Body`越しに値を渡す際に、カスケーディングパラメータを使います。
- セッション数を表示するコンポーネントはここに貼り付けられます。

```razor:Shared/MainLayout.razor
```

### ナビゲーションバー
- 一部内容がホームページのコンテンツとして再利用されます。
    - レイアウトから使われた場合は有効なパラメータが渡されていますが、ページから使われた場合はそれがありません。

```razor:Shared/NavBar.razor
```

### セッション数
- レイアウトレベルで、ページ毎に組み込まれるコンポーネントです。
    - 自身のインスタンスを活殺に合わせてリストし、その数を表示します。

```razor:Shared/SessionCounter.razor
```

- 行儀良く書くなら、別クラスのシングルトンサービスとして実装するべき(?)インスタンスの管理機構を、簡易に静的メンバーを使って済ませています。
- `IDisposable`を実装することで、破棄される際のトリガーを得ています。
- 別セッション(スレッド)での更新が他のセッションへ伝達された場合、そのタイミングで直接[`ComponentBase.StateHasChanged()`](https://learn.microsoft.com/ja-jp/dotnet/api/microsoft.aspnetcore.components.componentbase.statehaschanged?view=aspnetcore-7.0)を呼ぶことができません。
    - エラーして、`The current thread is not associated with the Dispatcher. Use InvokeAsync() to switch execution to the Dispatcher when triggering rendering or component state.`と表示されます。
    - 書かれている通りに[`ComponentBase.InvokeAsync()`](https://learn.microsoft.com/ja-jp/dotnet/api/microsoft.aspnetcore.components.componentbase.invokeasync?view=aspnetcore-7.0)を使うことで、同期コンテキスト(該当セッションのBlzaor UIスレッド)で動作させることができます。

### ホームページ
- ナビゲーションバーの一部内容をホームページのコンテンツとして再利用します。

```razor:Pages/Index.razor
```

## 一覧、詳細、編集、追加、削除
- 書籍と著者で同様のものを作るので、雛形を用意して、それを継承する形でそれぞれを作ります。
- ページは一覧の一つだけにして、他はダイアログとして実装します。
    - MudBlazorでは表のインライン編集も可能ですが、今回は使用しません。
- 一覧に複数項目の選択機能を付けて、一括削除を可能にします。
- 詳細ダイアログで閲覧/編集モードを切り替えるようにします。

- 

### 一覧
### 詳細と編集
### 追加
### 削除
### 一括削除

## おわりに
### 次に向けて
- (4)では、

### あとがき
- 執筆者は、Blazor、ASP.NETともに初学者ですので、誤りもあるかと思います。
    - お気づきの際は、是非コメントや編集リクエストにてご指摘ください。
    - あるいは、「それでも解らない」、「自分はこう捉えている」などといった、ご意見、ご感想も歓迎いたします。
