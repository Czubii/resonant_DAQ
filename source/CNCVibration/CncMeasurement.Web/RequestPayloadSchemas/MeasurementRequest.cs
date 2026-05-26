using Newtonsoft.Json;

namespace CncMeasurement.Web.RequestPayloadSchemas
{
    public class MeasurementRequest
    {
        [JsonProperty("Description")]
        public string Description { get; set; }
        [JsonProperty("Type")]
        // 1 - single measurement
        // 0 - multi measurement or whatever
        public int type;
        [JsonProperty("SampleRate")]
        public string SampleRate { get; set; }
        [JsonProperty("Duration")]
        public double Duration { get; set; }
        [JsonProperty("SpindleRPM")]
        public int SpindleRpm { get; set; }

    }
}
