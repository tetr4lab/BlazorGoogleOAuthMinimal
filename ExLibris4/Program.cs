using ExLibris4.Client.Pages;
using ExLibris4.Components;
using ExLibris4.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using WeatherCast.Data;

var builder = WebApplication.CreateBuilder (args);

// Add services to the container.
builder.Services.AddRazorComponents ()
    .AddInteractiveServerComponents ()
    .AddInteractiveWebAssemblyComponents ();
builder.Services.AddControllers ();
builder.Services.AddHttpClient ();

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

// 天気予報サービス
builder.Services.AddScoped<IWeatherForecastServices> (provider => new WeatherForecastServices ());

var app = builder.Build ();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment ()) {
    app.UseWebAssemblyDebugging ();
} else {
    app.UseExceptionHandler ("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts ();
}

// Application Base Path
var basePath = builder.Configuration ["AppBasePath"];
if (!string.IsNullOrEmpty (basePath)) {
    app.UsePathBase (basePath);
}

app.UseHttpsRedirection ();

app.UseStaticFiles ();
app.UseAntiforgery ();

app.UseAuthentication ();
app.UseAuthorization ();

app.MapControllers ();
app.MapRazorComponents<App> ()
    .AddInteractiveServerRenderMode ()
    .AddInteractiveWebAssemblyRenderMode ()
    .AddAdditionalAssemblies (typeof (ExLibris4.Client._Imports).Assembly);

System.Diagnostics.Debug.WriteLine ("Initialized");
app.Run ();
