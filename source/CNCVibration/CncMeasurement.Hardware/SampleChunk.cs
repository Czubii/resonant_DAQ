using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Hardware
{
    public class SampleChunk
    {
        public SampleChunk(double[] samples, int numSamples, long sampleIndex) 
        { 
            Samples = samples;
            NumSamples = numSamples;
            SampleIndex = sampleIndex;
        }

        public double[] Samples { get; }
        public int NumSamples { get; }
        public long SampleIndex { get; }
    }
}
