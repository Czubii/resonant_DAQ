using CncMeasurement.Core.models;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Processing
{
    public class FFTSpectrum
    {
        public static FftFrame Compute(SignalWindow signalWindow)
        {
            int channels = signalWindow.NumChannels;

            int n = signalWindow.Samples[0].Length;

            if ((n & (n - 1)) != 0)
                throw new ArgumentException("FFT size must be a power of 2");

            double[] window = Window.Hann(n);

            int half = n / 2;

            double[] frequencies = new double[half];
            double df = signalWindow.SampleRate / n;

            for (int i = 0; i < half; i++)
            {
                frequencies[i] = i * df;
            }

            var outputChannels = new FftChannel[channels];

            for (int ch = 0; ch < channels; ch++)
            {
                var buffer = new Complex[n];

                // apply windowing
                for (int i = 0; i < n; i++)
                {
                    buffer[i] = new Complex(
                        signalWindow.Samples[ch][i] * window[i],
                        0.0);
                }

                Fourier.Forward(buffer, FourierOptions.Matlab);

                var bins = new FftBin[half];

                for (int i = 0; i < half; i++)
                {
                    double mag = buffer[i].Magnitude;

                    mag *= 2.0 / n; // amplitude correction

                    bins[i] = new FftBin(mag);
                }

                outputChannels[ch] = new FftChannel(
                    signalWindow.AssignedChannelNames[ch],
                    bins);
            }

            return new FftFrame(
                signalWindow.SampleIndex,
                n,
                frequencies,
                signalWindow.TimeStamp,
                outputChannels);
        }
    }
}
