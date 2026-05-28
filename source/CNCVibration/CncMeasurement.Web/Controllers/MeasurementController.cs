using CncMeasurement.Core.models;
using CncMeasurement.Core.Interfaces;
using CncMeasurement.Data;
using CncMeasurement.Engine;
using CncMeasurement.Hardware;
using CncMeasurement.Hardware.Acquisition;
using CncMeasurement.Web.RequestPayloadSchemas;
using Microsoft.AspNetCore.Mvc;

namespace CncMeasurement.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MockEntryController : ControllerBase
    {
        private readonly IDatabaseController _dbController;

        // DI constructor
        public MockEntryController(IDatabaseController dbController)
        {
            _dbController = dbController;
        }

        [HttpGet(Name = "MockEntry")]
        public string Get()
        {
            // Generate the dummy Measurement and Graph data
            var mockData = new MeasurementMetadata
            {
                Timestamp = DateTime.Now,
                Description = "Automated Mock Calibration",
                Notes = "This entry was generated via the MockEntryController for testing.",
                Graphs = new GraphMetadata[]
                {
                    new GraphMetadata
                    {
                        Description = "Spindle Vibration",
                        Xaxis = "Time (ms)",
                        Yaxis = "Amplitude (mm/s)",
                        FilePath = @"C:\Data\Mock\vibration_test_1.tdms"
                    },
                    new GraphMetadata
                    {
                        Description = "Motor Temperature",
                        Xaxis = "Time (s)",
                        Yaxis = "Temperature (C)",
                        FilePath = @"C:\Data\Mock\temp_test_1.tdms"
                    }
                }
            };

            try
            {
                // 3. Save it to the database
                _dbController.AddMeasurementEntry(mockData);

                // 4. Return success confirmation
                return $"Success! Mock entry created with Timestamp: {mockData.Timestamp}";
            }
            catch (Exception ex)
            {
                // Good practice to catch errors here so you can see if the DB locked or failed
                return $"Failed to create mock entry. Error: {ex.Message}";
            }
        }
    }
    [ApiController]
    [Route("[controller]")]
    public class ListSummariesController : ControllerBase
    {
        private readonly IDatabaseController _dbController;

        // 1. Inject the Database Controller
        public ListSummariesController(IDatabaseController dbController)
        {
            _dbController = dbController;
        }

        // 2. Create the GET endpoint
        [HttpGet(Name = "GetSummaries")]
        public ActionResult<List<BriefMeasurementInfo>> Get()
        {
            try
            {
                // Fetch the lightweight summaries from the database
                var summaries = _dbController.GetMeasurementSummaries();

                // Return a 200 OK response containing the JSON data
                return Ok(summaries);
            }
            catch (Exception ex)
            {
                // Safely catch any database read errors and return a 500 status code
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }

    /// <summary>
    /// for testing: returns a sample JSON for a request
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class GetSampleRequestController : ControllerBase
    { 
        public string Get()
        {
            ExperimentRequest ex = new ExperimentRequest();

            ex.Description = "Example description";
            ex.MachineConfiguration = new MachineConfig();
            ex.MachineConfiguration.Y = 25;
            ex.Channels.Add(new AcquisitionConfig());
            return Newtonsoft.Json.JsonConvert.SerializeObject(ex);
        }
        // Dependency Injection pulls the service from your Program.cs registry
        

        /*[HttpPost("Name = Request measurement")]
        public async Task<IActionResult> Post([FromBody] MeasurementRequest Payload) {

            
        }*/
    }

    [ApiController]
    [Route("[controller]")]
    public class RequestExperimentController : ControllerBase
    {
        private readonly IEngine _engine;
        private ExperimentSetup _experimentSetup;
        public RequestExperimentController(IEngine engine)
        {
            _engine = engine;
        }
        public IActionResult Post([FromBody] ExperimentRequest payload)
        {
            //check if all properties are not null

            if(payload.GetType()
                   .GetProperties()
                   .Any(prop => prop.GetValue(payload) == null))
            {
                return BadRequest("Incorrect request payload");
            }
            _experimentSetup = payload.ToExperiment();
            _engine.LoadExperiment(_experimentSetup);
           
           return Ok($"Created experiment with ID {_experimentSetup.ID}"); 
        }
    }

    /* [ApiController]
     [Route("[controller]")]
     public class RequestSingleMeasurementController : ControllerBase
     {


         [HttpGet(Name = "RequestSingleMeasurement")]
         public async Task<IActionResult> Get()
         {
             var config = new MeasurementConfig() // TODO: Replace with parameters specified by user
             {
                 SampleRate = 1000,
                 DurationSeconds = 1,
                 ChannelName = "cDAQ1Mod1/ai0"
             };

             try
             {
                 double[] rawData = await _daqMeasurement.AcquireDataAsync(config);

                 return Ok(new
                 {
                     Timestamp = DateTime.UtcNow,
                     TotalSamples = rawData.Length,
                     Data = rawData
                 });
             }
             catch (Exception ex)
             {
                 return StatusCode(500, new { Message = ex.Message });
             }
         }
     }

     [ApiController]
     [Route("[controller]")]
     public class ListDevicesController : ControllerBase
     {
         private readonly IDaqDiscovery _DaqDiscovery;
         public ListDevicesController(IDaqDiscovery daqDiscovery)
         {
             _DaqDiscovery = daqDiscovery;
         }
         [HttpGet(Name = "ListDevices")]
         public ActionResult<List<DeviceDescription>> Get()
         {
             return Ok(_DaqDiscovery.GetAvailableDevices());
         }
     }*/
}
