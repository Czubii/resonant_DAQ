using CncMeasurement.Core.Interfaces;
using CncMeasurement.Core.models;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Timers;

namespace CncMeasurement.MockHardware
{
    /// <summary>
    /// Used for generating some test data to test the processing and main pipelines. 
    /// Doesn't generate the tdms output
    /// </summary>
    public class MockDataAcquisitionService: IDataAcquisitionService
    {
        private Channel<SampleChunk> _channel = Channel.CreateUnbounded<SampleChunk>();
        public ChannelReader<SampleChunk> Reader => _channel.Reader;

        private Task _acquisitionTask;
        private CancellationTokenSource _cts;

        private double _time;
        public Task StartAsync(AcquisitionConfig config, [EnumeratorCancellation] CancellationToken ct = default)
        {

            if (_acquisitionTask != null) throw new Exception("Acquisition Already Running");

            _time = 0.0;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            _channel = Channel.CreateUnbounded<SampleChunk>();

            _acquisitionTask = Task.Run(() => AcquisitionLoop(config, _cts.Token));

            return Task.CompletedTask;
        }

        private async Task AcquisitionLoop(AcquisitionConfig config, CancellationToken ct = default)
        {
            long sampleIdx = 0;
            int delayMs = (int)(1000.0 * config.ChunkSize / config.SampleRate);
            var startTimeUtc = DateTime.UtcNow;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    double[,] samples = GenerateWaveform(config);

                    int channels = samples.GetLength(0);
                    int count = samples.GetLength(1);

                    var timestamp = startTimeUtc.AddSeconds((double)sampleIdx / config.SampleRate);
                    _channel.Writer.TryWrite(new SampleChunk(sampleIdx, channels, count, timestamp, samples));

                    sampleIdx += count;

                    await Task.Delay(delayMs, ct); // a simple delay should be enough for testing
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

        private double[,] GenerateWaveform(AcquisitionConfig config)
        {
            int channels = config.ChannelConfigs.Count;
            int count = config.ChunkSize;

            double[,] samples = new double[channels, count];

            double dt = 1.0 / config.SampleRate;

            double baseFreq = 500.0;      // Hz
            double amplitude = 1.0;
            double noiseAmp = 0.02;

            var rand = Random.Shared;

            for (int i = 0; i < count; i++)
            {
                double t = _time + i * dt;

                for (int ch = 0; ch < channels; ch++)
                {
                    // phase shift per channel
                    double phase = ch * 0.5;

                    double signal =
                        amplitude * Math.Sin(2 * Math.PI * baseFreq * t + phase)
                        + 0.5 * Math.Sin(2 * Math.PI * (baseFreq * 3) * t + phase);

                    double noise = (rand.NextDouble() - 0.5) * 2.0 * noiseAmp;

                    samples[ch, i] = signal + noise;
                }
            }

            _time += count * dt;

            return samples;
        }
    }
}
