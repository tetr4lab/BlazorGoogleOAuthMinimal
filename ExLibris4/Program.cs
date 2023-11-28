using ExLibris4.Client.Pages;
using ExLibris4.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder (args);

// Add services to the container.
builder.Services.AddRazorComponents ()
    .AddInteractiveServerComponents ()
    .AddInteractiveWebAssemblyComponents ();

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

var app = builder.Build ();
var logger = app.Services.GetRequiredService<ILoggerFactory> ().CreateLogger<Program> ();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment ()) {
    app.UseWebAssemblyDebugging ();
} else {
    app.UseExceptionHandler ("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts ();
}

app.UseHttpsRedirection ();

app.UseStaticFiles ();
app.UseAntiforgery ();

app.UseAuthentication ();
app.UseAuthorization ();

app.MapRazorComponents<App> ()
    .AddInteractiveServerRenderMode ()
    .AddInteractiveWebAssemblyRenderMode ()
    .AddAdditionalAssemblies (typeof (Counter).Assembly);

logger.LogInformation ("Initialized");
app.Run ();
