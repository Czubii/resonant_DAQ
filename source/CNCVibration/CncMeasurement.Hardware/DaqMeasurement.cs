using CncMeasurement.Core.models;
using NationalInstruments.DAQmx;
using System;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;

namespace CncMeasurement.Hardware
{
    public interface IDaqMeasurement
    {
        Task<double[]> AcquireDataAsync(MeasurementConfig config);
    }
    public class DaqMeasurement : IDaqMeasurement
    {
        /// <summary>
        /// Captures a number of samples froms specified DAQ channel
        /// The number of samples taken will be n=config.DurationSeconds*config.SampleRate
        /// </summary>
        /// <param name="config">Measurement configuration</param>
        /// <returns>An array of raw measuremets</returns>
        public async Task<double[]> AcquireDataAsync(MeasurementConfig config)
        {

            return await System.Threading.Tasks.Task.Run(() =>
            {
                int nSamples = (int)(config.SampleRate * config.DurationSeconds);
                using (
                NationalInstruments.DAQmx.Task daqTask = new NationalInstruments.DAQmx.Task())
                {
                    try
                    {
                        daqTask.AIChannels.CreateVoltageChannel( //TODO check the channels
                            config.ChannelName,
                            "", // Name to assign to channel
                            AITerminalConfiguration.Pseudodifferential,
                            -5.0, // Minimum value expected
                            5.0,  // Maximum value expected
                            AIVoltageUnits.Volts
                        );
                        daqTask.Timing.ConfigureSampleClock(
                            "", // Use internal DAQ clock
                            config.SampleRate,
                            SampleClockActiveEdge.Rising,
                            SampleQuantityMode.FiniteSamples,
                            nSamples
                        );

                        // Verify that everything configured properly before starting hardware
                        daqTask.Control(TaskAction.Verify);

                        // Create the reader object bound to task stream
                        AnalogSingleChannelReader reader = new AnalogSingleChannelReader(daqTask.Stream);

                        // Start the physical hardware clock ticking
                        daqTask.Start();

                        double[] rawData = reader.ReadMultiSample(nSamples);

                        return rawData;
                    }
                    catch (DaqException ex)
                    {
                        throw new InvalidOperationException($"NI-DAQmx Hardware Failure: {ex.Message}", ex);
                    }
                    finally
                    {
                        // finally stop the task
                        try { daqTask.Stop(); } catch {}
                    }
                }
            });
        }
    }
}
