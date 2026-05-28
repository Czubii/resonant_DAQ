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

    /// <summary>
    /// Basic device information that allow for channel selection
    /// </summary>
    public class DeviceDescription
    {
        public string DeviceName { get; set; }
        public List<string> AIChannels { get; set; } 
        // This entry contains analog input channels of the device in the following form:
        //  cDAQ1Mod1/ai0
        //  cDAQ1Mod1/ai1
        //  cDAQ1Mod1/ai2
        //  cDAQ1Mod1/ai3
        // If no analog input channels are present for the device this list will be just empty

        public string ProductType { get; set; }
        public string SerialNumber { get; set; }
    }
}
