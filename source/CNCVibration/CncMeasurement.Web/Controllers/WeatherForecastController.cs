using Microsoft.AspNetCore.Mvc;
using CncMeasurement.Hardware;

namespace CncMeasurement.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        [HttpGet(Name = "GetWeatherForecast")]
        public string Get()
        {
            return commsTest.TEST();
        }
    }
}
