using System;
using System.Threading;
using System.Threading.Tasks;
using CncMeasurement.Core.models;
using NationalInstruments.DAQmx;

namespace CncMeasurement.Hardware
{
    public class DaqRecorder
    {
        /// <summary>
        /// Captures a number of samples froms specified DAQ channel
        /// The number of samples taken will be n=config.DurationSeconds*config.SampleRate
        /// </summary>
        /// <param name="config">Measurement configuration</param>
        /// <returns>An array of raw measuremets</returns>
        public double[] CaptureBlock(MeasurementConfig config)
        {
            int totalSamples = (int)(config.SampleRate * config.DurationSeconds);

            //TODO: Finish implementation, taking into account a proper configuration for the sensor
            double[] rawData = new double[totalSamples];

            return rawData;
        }
    }
}
