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
        public double[] Extract(double[] input, double sampleRateHz, double CenterFrequencyHz, double bandwidthHz)
        {
            int nSamples = input.Length;
            double[] output = new double[nSamples];
            Complex[] buffer = new Complex[nSamples];

            double standardDev = bandwidthHz / 2.355; // i love statistics and those "random" numbers

            for (int i = 0; i < nSamples; i++)
            {
                double w =
                    0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / (nSamples - 1));

                buffer[i] = new Complex(input[i] * w, 0.0); // copy to buffer and apply Hann windowing
            }

            Fourier.Forward(buffer, FourierOptions.Matlab);

            // apply the filtering:
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


            // finally reconstruct the (now filtered) signal
            Fourier.Inverse(buffer, FourierOptions.Matlab);

            for (int i = 0; i < nSamples; i++)
            {
                output[i] = buffer[i].Real;
            }

            return output;
        }
    }
}
