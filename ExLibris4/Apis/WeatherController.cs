using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherCast.Data;

namespace ExLibris4.Apis;

[Route ("api/[controller]")]
[Authorize (Policy = "Users")] // 対象に認可を与える
[ApiController]
public class WeatherController : Controller {

    // GET: api/weather
    [HttpGet]
    public async Task<IActionResult> List () {
        return Ok (await WeatherForecast.CreateAsync ());
    }

    // GET: api/weather/yyyy/mm/dd
    [HttpGet ("{year}/{month}/{day}")]
    public async Task<IActionResult> Item (int year, int month, int day) {
        try {
            return Ok (await WeatherForecast.CreateAsync (new DateOnly (year, month, day)));
        }
        catch (Exception ex) {
            return BadRequest (ex.Message);
        }
    }

    // POST: TestController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
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
