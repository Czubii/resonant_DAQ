using CncMeasurement.Core.models;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Processing
{
    public class FFTSpectrum
    {
        /// <summary>
        /// lists resonant frequencies based on PSD
        /// </summary>
        /// <param name="spectrum"></param>
        /// <returns></returns>
        public static (double ResonantFrequencyHz, double PeakAmplitude)[] ResonantFrequencies(FftFrame spectrum)
        {
            int nChannels = spectrum.Channels.Length;
            var output = new (double ResonantFrequencyHz, double PeakAmplitude)[nChannels];

            for (int ch = 0; ch < nChannels; ch++)
            {
                double largestAmplitude = 0.0;
                double resonantFrequ = 0.0;

                for (int i = 1; i < spectrum.Channels[ch].Magnitudes.Length; i++)
                {
                    var mag = spectrum.Channels[ch].PSDMagnitudes[i];
                    if (mag > largestAmplitude)
                    {
                        largestAmplitude = mag;
                        resonantFrequ = spectrum.FrequenciesHz[i];
                    }
                }
                output[ch] = new(resonantFrequ, largestAmplitude);
            }

            return output;
        }
        public static FftFrame Compute(SignalFrame signalWindow)
        {
            int channels = signalWindow.Channels.Length;

            int nRaw = signalWindow.Channels[0].Samples.Length;
            int n = NextPowerOfTwo(nRaw);

            double[] window = Window.Hann(nRaw); // window applied to real data

            // Calculate window gain correction factor
            // For Hann window, the sum of weights is roughly 0.5 * nRaw
            double windowSum = 0.0;
            for (int i = 0; i < nRaw; i++) windowSum += window[i];

            // Needed correction factor for PSD:
            double windowSqSum = 0.0;
            for (int i = 0; i < nRaw; i++) windowSqSum += window[i] * window[i];

            // PSD base scaling: |X|^2 / (sample_rate * sum(w^2))
            double psdScaling = 1.0 / (signalWindow.SampleRate * windowSqSum);

            // divided by windowSum to correct for Hann window attenuation.
            double magScaling = 1.0 / windowSum;

            int nBins = n / 2 + 1;

            double[] frequencies = new double[nBins];
            double df = signalWindow.SampleRate / n;

            for (int i = 0; i < nBins; i++)
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
                        signalWindow.Channels[ch].Samples[i] * window[i],
                        0.0);
                }

                // zero padding
                for (int i = nRaw; i < n; i++)
                {
                    buffer[i] = Complex.Zero;
                }

                Fourier.Forward(buffer, FourierOptions.Matlab);

                var mags = new double[nBins];
                var psdMags = new double[nBins];

                for (int i = 0; i < nBins; i++)
                {
                    double mag = buffer[i].Magnitude;

                    double scalingFactor = 2.0;
                    if (i == 0 || i ==  nBins - 1) scalingFactor = 1.0; // apply scaling to each frequenct except DC and nyquist

                    mags[i] = mag * magScaling * scalingFactor; // amplitude correction based on FFT size

                    double p = (mag * mag) * psdScaling * scalingFactor; // single sided psd

                    psdMags[i] = p;
                }

                outputChannels[ch] = new FftChannel(
                    signalWindow.Channels[ch].AssignedChannelName,
                    mags,
                    psdMags);
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
