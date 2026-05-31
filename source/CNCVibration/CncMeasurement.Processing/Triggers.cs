using CncMeasurement.Core.Interfaces;
using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Processing
{
    /// <summary>
    /// Very basic level trigger
    /// </summary>
    public class LevelTriggerDetector : Core.Interfaces.ITriggerDetector
    {
        private readonly double _threshold;

        public LevelTriggerDetector(double threshold)
        {
            _threshold = threshold;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns>true if absolute value of sample on any channel is over threshold </returns>
        public bool IsTriggered(SampleChunk chunk)
        {
            var data = chunk.Samples;

            int channels = data.GetLength(0);
            int samples = data.GetLength(1);

            for (int c = 0; c < channels; c++)
            {
                for (int i = 0; i < samples; i++)
                {
                    if (Math.Abs(data[c, i]) >= _threshold)
                        return true;
                }
            }

            return false;
        }
    }

}
