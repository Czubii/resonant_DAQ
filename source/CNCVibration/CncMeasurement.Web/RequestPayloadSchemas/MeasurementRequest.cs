using Newtonsoft.Json;
using CncMeasurement.Hardware.Acquisition;
using CncMeasurement.Core.models;

namespace CncMeasurement.Web.RequestPayloadSchemas
{
    public class ExperimentRequest
    {
        public string Description { get; set; }
        public List<AcquisitionConfig> Channels { get; set; }
        public MachineConfig MachineConfiguration { get; set; }

    }
}
