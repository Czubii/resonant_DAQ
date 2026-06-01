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

            int nRaw = signalWindow.Samples[0].Length;
            int n = NextPowerOfTwo(nRaw);

            double[] window = Window.Hann(nRaw); // window applied only to real data

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

                // apply window + copy signal
                for (int i = 0; i < nRaw; i++)
                {
                    buffer[i] = new Complex(
                        signalWindow.Samples[ch][i] * window[i],
                        0.0);
                }

                // zero padding
                for (int i = nRaw; i < n; i++)
                {
                    buffer[i] = Complex.Zero;
                }

                Fourier.Forward(buffer, FourierOptions.Matlab);

                var bins = new FftBin[half];

                for (int i = 0; i < half; i++)
                {
                    double mag = buffer[i].Magnitude;

                    mag *= 2.0 / n; // amplitude correction based on FFT size

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
        private static int NextPowerOfTwo(int n) // needed for padding the signal if number of samples is not power of 2 
        {
            int p = 1;
            while (p < n)
                p <<= 1;
            return p;
        }
    }
}
