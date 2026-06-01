using CncMeasurement.Core.Interfaces;
using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CncMeasurement.MockHardware
{
    public class ImpulseSignalGenerator : IDataAcquisitionService
    {
        private Channel<SampleChunk> _channel = Channel.CreateUnbounded<SampleChunk>();
        public ChannelReader<SampleChunk> Reader => _channel.Reader;

        private Task _acquisitionTask;
        private CancellationTokenSource _cts;

        private double _time;
        public Task Start(AcquisitionConfig config, [EnumeratorCancellation] CancellationToken ct = default)
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
            string[] assignedChannelNames = config.ChannelConfigs.Select(a => a.NameToAssignToChannel).ToArray();

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    double[,] samples = GenerateWaveform(config);

                    int channels = samples.GetLength(0);
                    int count = samples.GetLength(1);

                    var timestamp = startTimeUtc.AddSeconds((double)sampleIdx / config.SampleRate);
                    _channel.Writer.TryWrite(
                        new SampleChunk(sampleIdx, channels, assignedChannelNames, count, config.SampleRate, timestamp, samples));

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

            double t0 = 2.0;          // impulse time (seconds)
            double baseFreq = 500.0;  // vibration mode
            double amplitude = 20;
            double noiseAmp = 0.02;

            double damping = 50;     // exponential decay factor
            double impulseAmp = 25;   // excitation strength

            var rand = Random.Shared;

            for (int i = 0; i < count; i++)
            {
                double t = _time + i * dt;

                // 1. The sharp physical impact (Gaussian spike)
                double impulse = impulseAmp * Math.Exp(-Math.Pow((t - t0) / 0.002, 2));

                // 2. The structural response (Ring-down)
                double response = 0.0;

                if (t >= t0)
                {
                    double dT = t - t0; // Time elapsed since the hit

                    double structuralDamping = Math.Exp(-damping * dT);

                    // All structural frequencies/harmonics must be multiplied by the damping
                    double vibrations = amplitude * Math.Sin(2 * Math.PI * baseFreq * dT)
                                      + 0.5 * Math.Sin(2 * Math.PI * (baseFreq * 3) * dT);

                    response = vibrations * structuralDamping;
                }

                // 3. Combine everything per channel
                for (int ch = 0; ch < channels; ch++)
                {
                    double noise = (rand.NextDouble() - 0.5) * 2.0 * noiseAmp;
                    double chGain = 1.0 - ch * 0.1;

                    // The channel experiences the impulse impact + the structural ringing, plus noise
                    // Note: phase shifts per channel can be added inside the Sin functions above if needed
                    samples[ch, i] = chGain * (impulse + response) + noise;
                }
            }

            _time += count * dt;

            return samples;
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            _cts.Dispose();
        }
    }
}
