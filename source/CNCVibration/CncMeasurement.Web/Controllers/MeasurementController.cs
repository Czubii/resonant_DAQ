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
    public class GetDevicesController : ControllerBase
    {
        private readonly IDaqDiscovery _daqDiscovery;
        public GetDevicesController(IDaqDiscovery daqDiscovery)
        {
            _daqDiscovery = daqDiscovery;
        }
        [HttpGet(Name = "GetDevices")]
        public ActionResult<List<DeviceDescription>> Get()
        {
            return Ok(_daqDiscovery.GetAvailableDevices());
        }
    }
    [ApiController]
    [Route("[controller]")]
    public class UploadModalExperimentController : ControllerBase
    {
        private readonly IEngine _engine;

        public UploadModalExperimentController(IEngine engine)
        {
            _engine = engine;
        }
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ModalAnalysisRequest payload)
        {
            if (payload == null)
            {
                Console.WriteLine("[API] Received null payload in UploadModalExperimentController");
                return BadRequest("Request payload is null");
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("[API] Invalid experiment request recieved");
                return BadRequest(ModelState);
            }

            try
            {
                var experimentSetup = payload.ToExperiment();

                // Persist/load the setup into the engine
                await _engine.LoadExperiment(experimentSetup);

                // Start the modal experiment in background (fire-and-forget)
                _ = _engine.RunModalExperiment(CancellationToken.None);

                // Return 202 Accepted with a location header to the GetExperiment endpoint
                return AcceptedAtAction(
                    actionName: "Get",
                    controllerName: "GetExperiment",
                    routeValues: new { id = experimentSetup.ID },
                    value: new { id = experimentSetup.ID }
                );
            }
            catch (Exception ex)
            {
                // Log if logging is available; return generic problem response
                return Problem(detail: ex.Message);
            }
        }
    }

    [ApiController]
    [Route("[controller]")]
    public class  CheckExperimentStatusController : ControllerBase
    {
        private readonly IEngine _engine;

        public CheckExperimentStatusController(IEngine engine)
        {
            _engine = engine;
        }
        public IActionResult Get()
        {
            if (_engine.ModalStatus() == null)
            {
                return StatusCode(200, "The experiment is still running");
            }
            else return Ok(_engine.ModalStatus());
        }
    }
    [ApiController]
    [Route("[controller]")]
    public class GetAllExperimentsController : ControllerBase
    {
        private readonly IDatabaseController _dbhandler;
        public GetAllExperimentsController(IDatabaseController dbhandler)
        {
            _dbhandler = dbhandler;
        }
        [HttpGet(Name = "GetAllExperiments")]
        public async Task<IActionResult> Get()
        {
            var experiments = await _dbhandler.ListModalExperimentSchemaSummariesAsync();
            if (experiments == null || experiments.Count == 0)
            {
                return NotFound("No experiments found in the database");
            }
            return Ok(experiments);
        }
    }
    [ApiController]
    [Route("[controller]")]
    public class GetExperimentController : ControllerBase
    {
        private readonly IDatabaseController _dbhandler;
        public GetExperimentController(IDatabaseController dbhandler)
        {
            _dbhandler = dbhandler;
        }
        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Console.WriteLine("[API] GetExperimentController received null or empty ID");
                return BadRequest("ID cannot be null or empty");
            }

            if (!Guid.TryParse(id, out var guid))
            {
                Console.WriteLine("[API] GetExperimentController received invalid ID format: " + id);
                return BadRequest("Incorrect ID format");
            }

            var experiment = _dbhandler.GetModalExperimentSchema(guid);
            if (experiment == null)
            {

                return NotFound($"No experiment found with ID {id}");
            }

            // Return the typed object so ASP.NET performs a single correct JSON serialization
            return Ok(experiment);
        }
    }


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

        public async Task<string> Get()
        {
            var config = new AcquisitionConfig
            {
                SampleRate = 15000,
                ChunkSize = 4096,
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
                    },
                    new ChannelConfig
                    {
                        PhysicalChannelName = "cDAQ1Mod1/ai2",
                        NameToAssignToChannel = "Accel Z",
                        MinRange = -50,
                        MaxRange = 50,
                        Sensitivity = 100,
                    }
                }
            };

            var triggerConfig = new TriggerConfig
            {
                SampleRate = config.SampleRate,
                ChannelConfigs = config.ChannelConfigs,
                PreTriggerWindowMs = 250,
                PostTriggerWindowMs = 250,
                Threshold = 1.0
            };

            var analysisConfig = new ModalAnalysisConfig
            {
                ModeProminenceThresholddB = 2,
                DampingFilterBandwidthPercent = 0.1,
                DampingStartPeakPercent = 0.95f,
                DampingEndPeakPercent = 0.15f,
                UseNDominantModes = 8
            };
            ModalAnalysisRequest request = new ModalAnalysisRequest
            {
                Name = "Example Experiment",
                Description = "This is an example experiment setup for testing purposes.",
                MachineConfig = new MachineConfig
                {
                    Y = 25
                },
                MeasurementConfig = config,
                TriggerConfig = triggerConfig,
                AnalysisConfig = analysisConfig
            };

            var setup = request.ToExperiment();
            await _engine.LoadExperiment(setup);
            _engine.RunModalExperiment(CancellationToken.None);
            return $"running example experiment with ID {setup.ID}";
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
