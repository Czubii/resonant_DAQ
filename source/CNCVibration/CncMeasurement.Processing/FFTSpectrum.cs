using CncMeasurement.Core.models;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Processing
{
    /// <summary>
    /// For performing operations on fft spectra
    /// </summary>
    public class FFTSpectrum
    {
        /// <summary>
        /// lists resonant frequencies based on PSD
        /// </summary>
        /// <param name="spectrum"></param>
        /// <returns></returns>
        public static (double ResonantFrequencyHz, double PeakAmplitude)[] DominantFrequPerChannel(FftFrame spectrum)
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
        /// <summary>
        /// returns frequencies at which peaks appear in an combined PSD accross all channels. 
        /// Spectra are combined by using a maximum value at each bin.
        /// </summary>
        public static List<(int i, double frequency)> CombinedSpectrumPeaks(FftFrame spectrum, double prominenceThresholddB)
        {
            int nBins = spectrum.FrequenciesHz.Length;

            // Combine PSD-s and convert the magnitude to dB:
            double[] combined = new double[nBins];
            for (int i = 0; i < nBins; i++)
            {
                combined[i] = spectrum.Channels.Select(a => a.PSDMagnitudes[i]).ToArray().Max();
                combined[i] = 10*Math.Log10(combined[i]);
            }

            // smooth the combined spectrum a bit by using neighboring values:
            double[] smoothed = new double[nBins];
            for (int i = 0; i < nBins; i++)
            {
                if (i == 0 || i == nBins - 1)
                {
                    smoothed[i] = 0.0;
                }
                else
                {
                    smoothed[i] = (combined[i - 1] + combined[i] + combined[i + 1]) / 3;
                }
            }
            List<(int i, double value)> peaks = new List<(int i, double value)>();
            //loop through smoothed data and determine whether a point is a local max
            for (int i = 1; i < nBins-1; i++)
            {
                if (smoothed[i] > smoothed[i-1] && smoothed[i] > smoothed[i + 1])
                {
                    peaks.Add(new(i, smoothed[i]));
                }
            }
            double[] prominences = new double[peaks.Count];
            // calculate prominence of each peak:
            for (int p = 0; p<peaks.Count; p++)
            {
                var peak = peaks[p];
                double peakVal = smoothed[peak.i];
                // Find left bound (stop if we find a higher peak or hit the edge)
                int leftBound = 0;
                for (int i = peak.i - 1; i >= 0; i--)
                {
                    if (smoothed[i] > peakVal)
                    {
                        leftBound = i;
                        break;
                    }
                }
                // Find right bound (stop if we find a higher peak or hit the edge)
                int rightBound = nBins - 1;
                for (int i = peak.i + 1; i < nBins; i++)
                {
                    if (smoothed[i] > peakVal)
                    {
                        rightBound = i;
                        break;
                    }
                }
                // Find the minimum value within the left interval
                double leftMin = peakVal;
                for (int i = leftBound; i <= peak.i; i++)
                {
                    if (smoothed[i] < leftMin) leftMin = smoothed[i];
                }

                // Find the minimum value within the right interval
                double rightMin = peakVal;
                for (int i = peak.i; i <= rightBound; i++)
                {
                    if (smoothed[i] < rightMin) rightMin = smoothed[i];
                }
                double baseline = Math.Max(leftMin, rightMin);
                prominences[p] = peakVal - baseline;
            }

            var output = new List<(int i, double frequency)>();

            for (int p = 0; p < peaks.Count; p++)
            {
                var peak = peaks[p];
                var prominence = prominences[p];
                //Console.WriteLine($"frequency: {spectrum.FrequenciesHz[peak.i]:0.00}, value: {peak.value:0.00}, peominence: {prominence:0.00}");
                if (prominence > prominenceThresholddB)
                {
                    output.Add((peak.i, spectrum.FrequenciesHz[peak.i]));
                }
            }

            return output;
        }
    }
}
