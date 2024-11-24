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
        System.Diagnostics.Debug.WriteLine ("List");
        return Ok (await WeatherForecast.CreateAsync ());
    }

    // GET: api/weather/yyyy/mm/dd
    [HttpGet ("{year}/{month}/{day}")]
    public async Task<IActionResult> Item (int year, int month, int day) {
        System.Diagnostics.Debug.WriteLine ("Item");
        try {
            return Ok (await WeatherForecast.CreateAsync (new DateOnly (year, month, day)));
        }
        catch (Exception ex) {
            return BadRequest (ex.Message);
        }
    }

    // // POST: api/weather/post
    // [Route ("post")]
    // --- or ---
    // POST: api/weather
    [HttpPost]
    public ActionResult Post ([FromBody] WeatherForecast? forecast) {
        try {
            System.Diagnostics.Debug.WriteLine ($"Post {forecast}");
            return Ok (forecast);
        }
        catch (Exception ex) {
            return BadRequest (ex.Message);
        }
    }

}
