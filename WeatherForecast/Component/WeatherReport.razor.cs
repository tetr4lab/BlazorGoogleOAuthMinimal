using Microsoft.AspNetCore.Components;
using WeatherCast.Data;

namespace WeatherCast.Component;

/// <summary>天気予報の表示</summary>
/// <example>
/// &lt;Weather /&gt;
/// &lt;Weather Forecast="@forecast" Auto="false" /&gt;
/// &lt;Weather Forecasts="@forecasts" /&gt;
/// &lt;Weather Forecast="@forecast" Forecasts="@forecasts" /&gt;
/// </example>
public partial class WeatherReport {
    /// <summary>更新を親に伝える</summary>
    [Parameter] public EventCallback OnStateHasChanged { get; set; }

    /// <summary></summary>
    [Parameter] public WeatherForecast []? Forecasts { get; set; }

    /// <summary>予報</summary>
    [Parameter] public WeatherForecast? Forecast { get; set; }

    /// <summary>自動取得</summary>
    [Parameter] public bool? Auto { get; set; }

    /// <summary>初期化</summary>
    protected override void OnInitialized () {
        base.OnInitialized ();
        if (Auto is null) {
            Auto = Forecasts is null;
        }
    }

    protected int currentCount = 0;

    protected async Task IncrementCount () {
        System.Diagnostics.Debug.WriteLine ($"count {currentCount} => {currentCount + 1}");
        currentCount++;
        await Update ();
    }

    protected override async Task OnAfterRenderAsync (bool firstRender) {
        await base.OnAfterRenderAsync (firstRender);
        if (firstRender && Auto == true) {
            await Update ();
        }
    }

    protected async Task Update () {
        Forecasts = null;
        Forecasts ??= await ForecastServices.GetForecastsAsync ();
        if (OnStateHasChanged.HasDelegate) {
            await OnStateHasChanged.InvokeAsync ();
        }
        System.Diagnostics.Debug.WriteLine ("Weather initialized.");
    }

}
