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
        /// <param name="samples">Array of samples - one sample per channel</param>
        /// <returns>true if absolute value of sample on any channel is over threshold </returns>
        public bool IsTriggered(double[] samples)
        {
            int channels = samples.Length;

            for (int c = 0; c < channels; c++)
            {
                if (Math.Abs(samples[c]) >= _threshold)
                {
                    return true;
                }
            }
            return false;
        }
    }

}
