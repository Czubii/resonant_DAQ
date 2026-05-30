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

    public class MachineConfig
    {
        public int Y { get; set; }
    }

    public class ExperimentSetup
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public MachineConfig MachineConfiguration { get; set; }

        public AcquisitionConfig MeasurementConfig { get; set; }
        public int DurationMS { get; set; }
    }
        public class ChannelConfig
        {
            public string PhysicalChannelName { get; set; } //= "cDAQ1Mod1/ai0";
            public string NameToAssignToChannel { get; set; } // for example "Sensor Frame" or something idk
            public float Sensitivity { get; set; }
            public float MinRange { get; set; }
            public float MaxRange { get; set; }
        }
        public class AcquisitionConfig
        {
            public string GroupName;
            public string OutputTDMSPath;
            public List<ChannelConfig> ChannelConfigs { get; set; }
            public float SampleRate { get; set; } //= 10240.0;
            public int ChunkSize { get; set; } // optimal value will depend on sample rate. For 10kS/s 4096 should be an okay starting value
        }
        public sealed record SampleChunk
        (
            long SampleIndex,
            int NumChannels,
            int NumSamples,
            DateTime TimeStamp,
            double[,] Samples
        );
        public sealed record RmsFrame
        (
            long SampleIndex,
            DateTime Timestamp,
            RmsChannel[] Channels
        );
        public sealed record RmsChannel
        (
            int Channel,
            double Value
        );

}



