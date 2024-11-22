using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherCast.Data;

namespace ExLibris4.Apis;

[Route ("api/[controller]/[action]")]
[ApiController]
public class WeatherController : Controller {

    // 対象に認可を与える
    [Authorize (Policy = "Users")]
    [HttpGet]
    public async Task<IActionResult> List () {
        return Ok (await WeatherForecast.CreateAsync ());
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
