using CncMeasurement.Core.models;
using NationalInstruments.DAQmx;
using System;
using System.Buffers;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CncMeasurement.Hardware.Acquisition
{
    public interface IDataAcquisitionService
    {
        IAsyncEnumerable<SampleChunk> AcquireDataAsync(AcquisitionConfig config, [EnumeratorCancellation] CancellationToken ct = default);
    }
    public sealed class NIDataAcquisitionService : IDataAcquisitionService
    {
        /// <summary>
        /// Continuously Captures samples froms specified DAQ channel
        /// </summary>
        /// <param name="config">Measurement configuration</param>
        public async IAsyncEnumerable<SampleChunk> AcquireDataAsync(AcquisitionConfig config, [EnumeratorCancellation] CancellationToken ct = default)
        {
            using var daqTask = CreateTask(config);

            var reader = new AnalogSingleChannelReader(daqTask.Stream);

            ConfigureStream(daqTask, config);

            // Verify that everything configured properly before starting hardware
            daqTask.Control(TaskAction.Verify);

            daqTask.Start();

            long sampleIdx = 0;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    double[] samples = reader.ReadMultiSample(config.ChunkSize); // Yes, there is no better way

                    yield return new SampleChunk(samples, samples.Length, sampleIdx);

                    sampleIdx += config.ChunkSize;
                }
            }
            finally
            {
                daqTask.Stop();
                // No need to dispose of the task thanks to the using statement :0
            }
        }

        private static NationalInstruments.DAQmx.Task CreateTask(AcquisitionConfig config)
        {
            NationalInstruments.DAQmx.Task daqTask = new NationalInstruments.DAQmx.Task();

            // Create the channel for vibration/acceleration measurement
            daqTask.AIChannels.CreateAccelerometerChannel(
                "EXAMPLE CHANNEL NAME TO BE CHANGED", // TODO
                "", // Name to assign to channel for now empty but we may add something here later TODO
                AITerminalConfiguration.Differential,
                -50.0, // Minimum value expected in g
                50.0,  // Maximum value expected in g
                100.0, // Sensitivity in mV/g
                AIAccelerometerSensitivityUnits.MillivoltsPerG,
                AIExcitationSource.Internal,
                2.0, // Excitation current (for our sensor it's between 2mA and 20mA)
                AIAccelerationUnits.G
            );


            daqTask.Timing.ConfigureSampleClock(
                "", // Use internal DAQ clock
                config.SampleRate,
                SampleClockActiveEdge.Rising, // Sample on rising clock edge
                SampleQuantityMode.ContinuousSamples,
                config.ChunkSize // in case of continuous acquisition this will act as buffer segmentation size
            );


            return daqTask;
        }


        private static void ConfigureStream(NationalInstruments.DAQmx.Task daqTask, AcquisitionConfig config)
        {
            // Make the input buffer a bit larger than the chunk size in case CPU has problems to keep up with the data
            daqTask.Stream.Buffer.InputBufferSize = config.ChunkSize * 32;

            daqTask.Stream.ReadOverwriteMode = ReadOverwriteMode.DoNotOverwriteUnreadSamples;

            daqTask.Stream.Timeout = Timeout.Infinite;
        }
    }
}
