using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using MathNet.Numerics.Statistics;

namespace CncMeasurement.Processing
{
    public interface ILiveSignalProcessor: IAsyncDisposable
    {
        public Task Start(ChannelReader<SampleChunk> sampleChunkReader, CancellationToken ct = default);
        public Task StopAsync();
        public ChannelReader<int> FFTReader { get; }
        public ChannelReader<double> RMSReader { get; }
    }
    public class LiveSignalProcessor : ILiveSignalProcessor
    {
        private readonly CancellationTokenSource _cts = new();

        private Task? _processingTask;

        private Channel<int> _fftChannel = Channel.CreateUnbounded<int>();
        private Channel<double> _rmsChannel = Channel.CreateUnbounded<double>();
        public ChannelReader<int> FFTReader => _fftChannel.Reader;
        public ChannelReader<double> RMSReader => _rmsChannel.Reader;

        public Task Start(ChannelReader<SampleChunk> reader, CancellationToken ct = default)
        {
            if (_processingTask != null) throw new InvalidOperationException("Already started.");

            var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);

            _processingTask = RunAsync(reader, linked.Token);

            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            _cts.Cancel();

            if (_processingTask != null)
                await _processingTask;
        }

        private async Task RunAsync(
            ChannelReader<SampleChunk> reader,
            CancellationToken ct)
        {
            await foreach (var chunk in reader.ReadAllAsync(ct))
            {
                int channels = chunk.NumChannels;
                int samples = chunk.NumSamples;

                for (int ch = 0; ch < channels; ch++)
                {
                    double sum = 0.0;

                    for (int i = 0; i < samples; i++)
                    {
                        double x = chunk.Samples[ch, i];
                        sum += x * x;
                    }

                    double rms = Math.Sqrt(sum / samples);

                    _rmsChannel.Writer.TryWrite(rms);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            _cts.Dispose();
        }
    }
}
