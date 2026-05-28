using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using MathNet.Numerics.Statistics;
using CncMeasurement.Core.Interfaces;

namespace CncMeasurement.Processing
{
    public class LiveSignalProcessor : ILiveSignalProcessor
    {
        private CancellationTokenSource _cts = new();

        private Task? _processingTask;

        private Channel<int> _fftChannel = Channel.CreateUnbounded<int>();
        private Channel<RmsFrame> _rmsChannel = Channel.CreateUnbounded<RmsFrame>();
        public ChannelReader<int> FFTReader => _fftChannel.Reader;
        public ChannelReader<RmsFrame> RMSReader => _rmsChannel.Reader;

        public Task Start(ChannelReader<SampleChunk> reader, CancellationToken ct = default)
        {
            if (_processingTask != null) throw new InvalidOperationException("Already started.");

            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            _fftChannel = Channel.CreateUnbounded<int>();
            _rmsChannel = Channel.CreateUnbounded<RmsFrame>();

            _processingTask = RunAsync(reader, _cts.Token);

            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            _cts.Cancel();

            if (_processingTask != null)
                await _processingTask;

            _processingTask = null;
        }

        private async Task RunAsync(ChannelReader<SampleChunk> reader, CancellationToken ct)
        {
            try 
            { 
                await foreach (var chunk in reader.ReadAllAsync(ct))
                {
                    WriteRMS(chunk);
                }
            }
            catch (OperationCanceledException){}
            finally
            {
                _rmsChannel.Writer.TryComplete();
                _fftChannel.Writer.TryComplete();
            }
        }

        private void WriteRMS(SampleChunk chunk)
        {
            int channels = chunk.NumChannels;
            int samples = chunk.NumSamples;

            RmsChannel[] rmsChannel = new RmsChannel[channels];

            for (int ch = 0; ch < channels; ch++)
            {
                double sum = 0.0;

                for (int i = 0; i < samples; i++)
                {
                    double x = chunk.Samples[ch, i];
                    sum += x * x;
                }

                double rms = Math.Sqrt(sum / samples);

                rmsChannel[ch] = new RmsChannel(chunk.assignedChannelNames[ch], rms);
            }
            _rmsChannel.Writer.TryWrite(new RmsFrame(chunk.SampleIndex, chunk.TimeStamp, rmsChannel));
        }
        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            _cts.Dispose();
        }
    }
}
