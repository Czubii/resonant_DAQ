using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using CncMeasurement.Core.models;

namespace CncMeasurement.Hardware
{
    public class DaqRecorder
    {
        /// <summary>
        /// Captures a number of samples froms specified DAQ channel
        /// The number of samples taken will be n=config.DurationSeconds*config.SampleRate
        /// </summary>
        /// <param name="config">Measurement configuration</param>
        /// <returns></returns>
        public double[] CaptureBlock(MeasurementConfig config)
        {
            int totalSamples = (int)(config.SampleRate * config.DurationSeconds);

            //TODO: Finish implementation, taking into account a proper configuration for the sensor
        }
    }
}
