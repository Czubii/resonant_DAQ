using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Processing
{
    /// <summary>
    /// simplest possible band pass filter that transforms the signal into frequency domain, applies filtering, 
    /// and then reconstructs it
    /// </summary>
    public static class FFTBandPass
    {
        public static double[] Apply(double[] input, double sampleRateHz, double CenterFrequencyHz, double bandwidthHz)
        {
            int nSamples = input.Length;
            double[] output = new double[nSamples];

            double standardDev = bandwidthHz / 2.355; // i love statistics and those "random" numbers

            double[] window = Window.Hann(nSamples); // window applied to real data
            var buffer = FFTProcessor.GenerateFFTBuffer(input,nSamples,nSamples, window);

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

                double weight =Math.Exp( -0.5 *Math.Pow((Math.Abs(frequ) - CenterFrequencyHz) / standardDev,2)); // gaussian distribution

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
