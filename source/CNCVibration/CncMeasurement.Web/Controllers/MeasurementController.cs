using CncMeasurement.Core.models;
using CncMeasurement.Core.Interfaces;
using CncMeasurement.Data;
using CncMeasurement.Engine;
using CncMeasurement.Web.RequestPayloadSchemas;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CncMeasurement.Web.Controllers
{
    
    [ApiController]
    [Route("[controller]")]
    public class RunExampleExperimentController : ControllerBase
    {
        private readonly IEngine _engine;
        private ExperimentSetup _setup;
        public RunExampleExperimentController(IEngine engine)
        {
            _engine = engine;
        }

        public string Get()
        {
            ExperimentRequest ex = new ExperimentRequest();
            ex.Name = "Example name";
            ex.Description = "Example description";
            ex.MachineConfig = new MachineConfig
            {
                Y = 25
            };
            ex.MeasurementConfig = new AcquisitionConfig
            {
                SampleRate = 10000,
                ChunkSize = 1000,
                GroupName = "test",
                OutputTDMSPath = "tetstoutput.tdms",
                ChannelConfigs = new List<ChannelConfig>
            {
                new ChannelConfig
                {
                    PhysicalChannelName = "cDAQ1Mod1/ai0",
                    NameToAssignToChannel = "Accel X",
                    MinRange = -50,
                    MaxRange = 50,
                    Sensitivity = 100,

                },
                new ChannelConfig
                {
                    PhysicalChannelName = "cDAQ1Mod1/ai1",
                    NameToAssignToChannel = "Accel Y",
                    MinRange = -50,
                    MaxRange = 50,
                    Sensitivity = 100,
                }
            }
            };

            _setup = ex.ToExperiment();
            _engine.LoadExperiment(_setup);
            _engine.RunExperiment(CancellationToken.None);
            return "running example experiment";
        }

    }

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
            return JsonConvert.SerializeObject(mockData);
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
            ex.Name = "Example name";
            ex.Description = "Example description";
            ex.MachineConfig = new MachineConfig
            {
                Y = 25
            };
            ex.MeasurementConfig = new AcquisitionConfig
            {
                SampleRate = 10000,
                ChunkSize = 1000,
                GroupName = "test",
                OutputTDMSPath = "tetstoutput.tdms",
                ChannelConfigs = new List<ChannelConfig>
            {
                new ChannelConfig
                {
                    PhysicalChannelName = "cDAQ1Mod1/ai0",
                    NameToAssignToChannel = "Accel X",
                    MinRange = -50,
                    MaxRange = 50,
                    Sensitivity = 100,

                },
                new ChannelConfig
                {
                    PhysicalChannelName = "cDAQ1Mod1/ai1",
                    NameToAssignToChannel = "Accel Y",
                    MinRange = -50,
                    MaxRange = 50,
                    Sensitivity = 100,
                }
            }
            };
            return JsonConvert.SerializeObject(ex);
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

                if (payload.GetType()
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

    //public class GetSensorsController
    //{
    //    public IDaqDiscovery _daqdiscovery { get; set; }
    //    public string Get()
    //    {
    //        return JsonConvert.SerializeObject(_daqdiscovery.GetAvailableDevices());
    //    }
    //}

}
