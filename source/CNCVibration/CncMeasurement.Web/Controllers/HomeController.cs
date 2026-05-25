using CncMeasurement.Hardware;
using Microsoft.AspNetCore.Mvc;

namespace CncMeasurement.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetSummariesController : ControllerBase
    {
        [HttpGet(Name = "")]
        public string Get()
        {
            return commsTest.TEST();
        }
    }
}
