using Microsoft.AspNetCore.Authorization;
using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ExLibris4.Weather;

namespace ExLibris4.Apis;

[Route ("api/[controller]/[action]")]
[ApiController]
public class WeatherController : Controller {

    // 対象に認可を与える
    [Authorize (Policy = "Users")]
    [HttpGet]
    //[ValidateAntiForgeryToken]
    public async Task<IActionResult> List () {
        var startDate = DateOnly.FromDateTime (DateTime.Now);
        var summaries = new [] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
        var forecasts = Enumerable.Range (1, 5).Select (index => new WeatherForecast {
            Date = startDate.AddDays (index),
            TemperatureC = Random.Shared.Next (-20, 55),
            Summary = summaries [Random.Shared.Next (summaries.Length)]
        }).ToArray ();
        forecasts = await WeatherForecast.Create ();
        return Ok (forecasts);
    }

    // POST: TestController/Create
    //[Authorize (Policy = "Users")]
    [HttpPost]
    //[ValidateAntiForgeryToken]
    public ActionResult Create (IFormCollection collection) {
        try {
            return RedirectToAction (nameof (Index));
        }
        catch {
            return View ();
        }
    }

    // GET: TestController/Edit/5
    public ActionResult Edit (int id) {
        return View ();
    }

    // POST: TestController/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Edit (int id, IFormCollection collection) {
        try {
            return RedirectToAction (nameof (Index));
        }
        catch {
            return View ();
        }
    }

    // GET: TestController/Delete/5
    public ActionResult Delete (int id) {
        return View ();
    }

    // POST: TestController/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Delete (int id, IFormCollection collection) {
        try {
            return RedirectToAction (nameof (Index));
        }
        catch {
            return View ();
        }
    }
}
