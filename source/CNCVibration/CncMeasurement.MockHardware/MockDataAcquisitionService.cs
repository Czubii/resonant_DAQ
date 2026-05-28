using CncMeasurement.Core.models;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace CncMeasurement.MockHardware
{
    public class MockDataAcquisitionService
    {
        private Channel<SampleChunk> _channel = Channel.CreateUnbounded<SampleChunk>();
        public ChannelReader<SampleChunk> Reader => _channel.Reader;

        private Task _acquisitionTask;
        private CancellationTokenSource _cts;
        public Task StartAsync(AcquisitionConfig config, [EnumeratorCancellation] CancellationToken ct = default)
        {

            if (_acquisitionTask != null) throw new Exception("Acquisition Already Running");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            _channel = Channel.CreateUnbounded<SampleChunk>();

            _acquisitionTask = Task.Run(() => AcquisitionLoop(config, _cts.Token));

            return Task.CompletedTask;
        }

        private async Task AcquisitionLoop(AcquisitionConfig config, CancellationToken ct = default)
        {
            long sampleIdx = 0;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    double[,] samples = _reader.ReadMultiSample(config.ChunkSize); // Yes, there is no better way

                    int channels = samples.GetLength(0);
                    int count = samples.GetLength(1);

                    _channel.Writer.TryWrite(new SampleChunk(samples, channels, count, sampleIdx));

                    sampleIdx += count;
                }
            }
            catch (Exception ex)
            {
                _channel.Writer.TryComplete(ex);
            }
            finally
            {
                _channel.Writer.TryComplete();
            }
        }

        public async Task StopAsync()
        {
            if (_acquisitionTask == null) return;

            _cts?.Cancel();

            try
            {
                await _acquisitionTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }


            _channel.Writer.TryComplete();

            _acquisitionTask = null;

            _cts?.Dispose();
            _cts = null;
        }

        private static NationalInstruments.DAQmx.Task CreateTask(AcquisitionConfig config)
        {
            NationalInstruments.DAQmx.Task daqTask = new NationalInstruments.DAQmx.Task();

            // Configure the channels for acceleration measurement:

            foreach (var ch in config.ChannelConfigs)
            {
                daqTask.AIChannels.CreateAccelerometerChannel(
                    ch.PhysicalChannelName,
                    ch.NameToAssignToChannel,
                    AITerminalConfiguration.Pseudodifferential,
                    ch.MinRange, // Minimum value expected in g
                    ch.MaxRange,  // Maximum value expected in g
                    ch.Sensitivity, // Sensitivity in mV/g
                    AIAccelerometerSensitivityUnits.MillivoltsPerG,
                    AIExcitationSource.Internal,
                    0.002, // Excitation current (for our sensor it's between 2mA and 20mA)
                    AIAccelerationUnits.G
                );
            }

            daqTask.Timing.ConfigureSampleClock(
                "", // Use internal DAQ clock
                config.SampleRate,
                SampleClockActiveEdge.Rising, // Sample on rising clock edge
                SampleQuantityMode.ContinuousSamples,
                config.ChunkSize // in case of continuous acquisition this will act as buffer segmentation size
            );


            return daqTask;
        }
    }
}
