using System;
using System.Text.Json.Serialization;

namespace CncMeasurement.Core.models
{
    // general information about the database collections
    public class DBinfo
    {
        public string[] Collections;
    }
    // General measurement data
    public class MeasurementMetadata
    {
        public int ID { get; set; }
        public DateTime Timestamp { get; set; }
        public GraphMetadata[] Graphs { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }
    }
    // for storing the informations about a .TDMS graph
    public class GraphMetadata
    {

        public string Description { get; set; }
        public string Xaxis { get; set; }
        public string Yaxis { get; set; }
        public string FilePath { get; set; }
    }
    public class BriefMeasurementInfo
    {
        public int ID { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
    }

    public class MeasurementConfig
    {
        public string ChannelName { get; set; } = "cDAQ1Mod1/ai0";
        public double SampleRate { get; set; } = 10240.0;
        public double DurationSeconds { get; set; } = 2.0;
        public int SpindleRpm { get; set; }
    }

    /// <summary>
    /// Basic device information that allow for channel selection
    /// </summary>
    public class DeviceDescription
    {
        public string DeviceName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
    }
}
