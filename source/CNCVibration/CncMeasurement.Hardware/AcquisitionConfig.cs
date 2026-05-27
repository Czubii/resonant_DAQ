using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Hardware.Acquisition
{
    public class AcquisitionConfig
    {
        public string ChannelName { get; set; } //= "cDAQ1Mod1/ai0";
        public float SampleRate { get; set; } //= 10240.0;
        public float DurationSeconds { get; set; } //= 2.0;
        public int ChunkSize { get; set; } // optimal value will depend on sample rate. For 10kS/s 4096 should be an okay starting value
    }
}
