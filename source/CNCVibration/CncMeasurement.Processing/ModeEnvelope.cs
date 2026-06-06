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
    /// Extracts a Hilbert envelope of a signal after applying a band-pass filter
    /// </summary>
    public class ModeEnvelope()
    {
        public static double[] Extract(double[] input, double sampleRateHz, double CenterFrequencyHz, double bandwidthHz)
        {
            int nSamples = input.Length;

            double standardDev = bandwidthHz / 2.355; // i love statistics and those "random" numbers

            var buffer = FFTConverter.GenerateFFTBuffer(input, nSamples, nSamples);

            Fourier.Forward(buffer, FourierOptions.Matlab);

            // Gauusian band-pass:
            for (int k = 0; k < nSamples; k++)
            {
                double frequ = 0.0;
                if (k <= nSamples / 2)
                {
                    // Positive-frequency half of the spectrum (including DC and Nyquist).
                    frequ = k * sampleRateHz / nSamples;
                }
                else
                {
                    // Negative-frequency half of the spectrum.
                    frequ = (k - nSamples) * sampleRateHz / nSamples;
                }

                double weight = Math.Exp(-0.5 * Math.Pow((Math.Abs(frequ) - CenterFrequencyHz) / standardDev, 2)); // gaussian distribution

                buffer[k] *= weight;
            }


            // Hilbert transform in frequency domain:
            if ((nSamples & 1) == 0) // even
            {
                for (int k = 1; k < nSamples / 2; k++)
                    buffer[k] *= 2.0;

                for (int k = nSamples / 2 + 1; k < nSamples; k++)
                    buffer[k] = Complex.Zero;
            }
            else // odd
            {
                for (int k = 1; k <= (nSamples - 1) / 2; k++)
                    buffer[k] *= 2.0;

                for (int k = (nSamples + 1) / 2; k < nSamples; k++)
                    buffer[k] = Complex.Zero;
            }

            
            Fourier.Inverse(buffer, FourierOptions.Matlab);

            // finally we can extract the envelope:
            double[] envelope = new double[nSamples];

            for (int i = 0; i < nSamples; i++)
            {
                envelope[i] = buffer[i].Magnitude;
            }

            return envelope;
        }
    }
}
