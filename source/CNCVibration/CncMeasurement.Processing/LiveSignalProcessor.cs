using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using MathNet.Numerics.Statistics;
using CncMeasurement.Core.Interfaces;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace CncMeasurement.Processing
{
    /// <summary>
    /// New channel is being created each time you start the processing!!!
    /// </summary>
    public class LiveSignalProcessor : ILiveSignalProcessor
    {
        private CancellationTokenSource _cts = new();

        private Task? _processingTask;

        private Channel<FftFrame> _fftChannel = Channel.CreateUnbounded<FftFrame>();
        private Channel<RmsFrame> _rmsChannel = Channel.CreateUnbounded<RmsFrame>();
        public ChannelReader<FftFrame> FFTReader => _fftChannel.Reader;
        public ChannelReader<RmsFrame> RMSReader => _rmsChannel.Reader;

        public Task Start(ChannelReader<SampleChunk> reader, CancellationToken ct = default)
        {
            if (_processingTask != null) throw new InvalidOperationException("Already started.");

            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            _fftChannel = Channel.CreateUnbounded<FftFrame>();
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
                    if (chunk.NumSamples == 0) continue;

                    _rmsChannel.Writer.TryWrite(ComputeRMS(chunk));
                    _fftChannel.Writer.TryWrite(ComputeFft(chunk));
                }
            }
            catch (OperationCanceledException){}
            finally
            {
                _rmsChannel.Writer.TryComplete();
                _fftChannel.Writer.TryComplete();
            }
        }
        private RmsFrame ComputeRMS(SampleChunk chunk)
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

                rmsChannel[ch] = new RmsChannel(chunk.AssignedChannelNames[ch], rms);
            }
            return new RmsFrame(chunk.SampleIndex, chunk.TimeStamp, rmsChannel);
        }
        private FftFrame ComputeFft(SampleChunk chunk)
        {
            int n = chunk.NumSamples;
            int channels = chunk.NumChannels;

            if ((n & (n - 1)) != 0)
                throw new ArgumentException("FFT size must be power of 2");

            var window = CreateHannWindow(n); // TODO: buffer this somwehere for entire processing

            int half = n / 2;

            double[] frequencies = new double[half];
            double df = chunk.SampleRate / n;

            for (int i = 0; i<half; i++)
            {
                frequencies[i] = i * df;
            }

            var outputChannels = new FftChannel[channels];

            for (int ch = 0;  ch < channels; ch++)
            {
                var buffer = new Complex[n];

                // apply windowing:
                for (int i = 0; i < n; i++)
                {
                    buffer[i] = new Complex(chunk.Samples[ch, i] * window[i], 0.0);
                }

                // fourier transform:
                Fourier.Forward(buffer, FourierOptions.Matlab);

                // Extracting magnitudes:

                var bins = new FftBin[half];

                for (int i = 0; i < half; i++)
                {
                    double mag = buffer[i].Magnitude;

                    mag *= 2.0 / n; // amplitud correction

                    bins[i] = new FftBin(mag);
                }

                outputChannels[ch] = new FftChannel(chunk.AssignedChannelNames[ch], bins);

            }

            return new FftFrame(chunk.SampleIndex, n, frequencies, chunk.TimeStamp, outputChannels);
        }
        private static double[] CreateHannWindow(int n)
        {
            var w = new double[n];
            for (int i = 0; i < n; i++)
                w[i] = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (n - 1)));
            return w;
        }
        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            _cts.Dispose();
        }
    }
}
