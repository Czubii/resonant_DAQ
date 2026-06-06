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
    /// <summary>
    /// wrapper for the fft computations
    /// </summary>
    public class FFTConverter
    {

        /// <summary>
        /// 
        /// </summary>
        public static double WindowSum(double[] window, int n)
        {
            double windowSum = 0.0;
            for (int i = 0; i < n; i++) windowSum += window[i];
            return windowSum;
        }
        /// <summary>
        /// Needed correction factor for PSD:
        /// </summary>
        public static double WindowSqSum(double[] window, int n)
        {
            double windowSum = 0.0;
            for (int i = 0; i < n; i++) windowSum += window[i]*window[i];
            return windowSum;
        }
        /// <summary>
        /// Used to correct for window attenuation.
        /// </summary>
        public static double MagnitudeCorrection(double[] window, int n)
        {
            double windowSum = WindowSum(window, n);
            return 1.0 / windowSum;
        }

        /// <summary>
        /// PSD base scaling: |X|^2 / (sample_rate * sum(w^2))
        /// </summary>
        public static double PSDCorrection(double[] window, int n, double sampleRate)
        {
            double windowSqSum = WindowSqSum(window, n);
            return 1.0 / (sampleRate * windowSqSum);
        }

        public static double[] GenerateFrequencyBins(int nBins, int fftSize, double sampleRate)
        {
            double[] frequencies = new double[nBins];
            double df = sampleRate / fftSize;

            for (int i = 0; i < nBins; i++)
            {
                frequencies[i] = i * df;
            }

            return frequencies;
        }

        /// <summary>
        /// Copies real valued samples into a complex buffer. Automatically pads the size of the output with zeros, to keep the FFT size a power of two.
        /// Allows to apply windowing by passing the coefficients
        /// </summary>
        public static Complex[] GenerateFFTBuffer(double[] realSamples, int nRealSamples, int fftSize, double[]? window = null)
        {
            var outputBuffer = new Complex[fftSize];

            for (int i = 0; i < nRealSamples; i++)
            {
                double w = (window != null && i < window.Length) ? window[i] : 1.0;

                outputBuffer[i] = new Complex(realSamples[i] * w, 0.0);
            }

            // zero padding
            for (int i = nRealSamples; i < fftSize; i++)
            {
                outputBuffer[i] = Complex.Zero;
            }

            return outputBuffer;
        }

        public static FftFrame ComputeFrame(SignalFrame signalWindow)
        {
            int channels = signalWindow.Channels.Length;

            int nRaw = signalWindow.Channels[0].Samples.Length;
            int n = NextPowerOfTwo(nRaw);

            double[] window = Window.Hann(nRaw); // window applied to real data

            double psdScaling = PSDCorrection(window, nRaw, signalWindow.SampleRateHz);
            double magScaling = MagnitudeCorrection(window, nRaw);

            int nBins = n / 2 + 1; // We only want the positive frequencies, and also lets keep the dc and nyquist

            double[] frequencies = GenerateFrequencyBins(nBins, n, signalWindow.SampleRateHz);

            var outputChannels = new FftChannel[channels];

            for (int ch = 0; ch < channels; ch++)
            {
                var buffer = GenerateFFTBuffer(signalWindow.Channels[ch].Samples, nRaw, n, window);

                Fourier.Forward(buffer, FourierOptions.Matlab);

                var mags = new double[nBins];
                var psdMags = new double[nBins];

                for (int i = 0; i < nBins; i++)
                {
                    double mag = buffer[i].Magnitude;

                    double scalingFactor = 2.0;
                    if (i == 0 || i == nBins - 1) scalingFactor = 1.0; // apply scaling to each frequenct except DC and nyquist

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
