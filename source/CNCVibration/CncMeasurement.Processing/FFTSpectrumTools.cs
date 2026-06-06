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
    public class FFTSpectrumTools
    {
        /// <summary>
        /// lists resonant frequencies based on PSD
        /// </summary>
        /// <param name="spectrum"></param>
        /// <returns></returns>
        public static (double ResonantFrequencyHz, double PeakAmplitude)[] DetectDominantFrequPerChannel(FftFrame spectrum)
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
        public static List<(int i, double frequency)> DetectCombinedSpectrumPeaks(FftFrame spectrum, double prominenceThresholddB, 
            int maxPeaks = 5)
        {
            int nBins = spectrum.FrequenciesHz.Length;

            // Combine PSD-s and convert the magnitude to dB:
            double[] combined = new double[nBins];
            for (int i = 0; i < nBins; i++)
            {
                combined[i] = spectrum.Channels.Max(a => 10 * Math.Log10(Math.Max(a.PSDMagnitudes[i], 1e-12)));
            }

            // smooth the combined spectrum a bit by using neighboring values:
            double[] smoothed = new double[nBins];
            for (int i = 0; i < nBins; i++)
            {
                if (i == 0 || i == nBins - 1)
                {
                    smoothed[i] = combined[i]; // copy at the edges
                }
                else if (i == 1 || i == nBins - 2)
                {
                    smoothed[i] = (combined[i - 1] + combined[i] + combined[i + 1]) / 3;
                }
                else
                {
                    smoothed[i] = (combined[i - 2] + combined[i - 1] + combined[i] + combined[i + 1] + combined[i + 2]) / 5;
                }
            }
            var peaks = new List<(int i, double value)>();
            //loop through smoothed data and determine whether a point is a local max
            for (int i = 1; i < nBins-1; i++)
            {
                if (smoothed[i] > smoothed[i-1] && smoothed[i] > smoothed[i + 1])
                {
                    peaks.Add(new(i, smoothed[i]));
                }
            }
            var candidates = new List<(int i, double frequency, double powerDb)>();
            // calculate prominence of each peak:
            foreach (var peak in peaks)
            {
                double peakVal = smoothed[peak.i];

                int leftBound = 0;
                for (int i = peak.i - 1; i >= 0; i--)
                {
                    if (smoothed[i] > peakVal)
                    {
                        leftBound = i;
                        break;
                    }
                }

                int rightBound = nBins - 1;
                for (int i = peak.i + 1; i < nBins; i++)
                {
                    if (smoothed[i] > peakVal)
                    {
                        rightBound = i;
                        break;
                    }
                }

                double leftMin = peakVal;
                for (int i = leftBound; i <= peak.i; i++)
                    leftMin = Math.Min(leftMin, smoothed[i]);

                double rightMin = peakVal;
                for (int i = peak.i; i <= rightBound; i++)
                    rightMin = Math.Min(rightMin, smoothed[i]);

                double prominence = peakVal - Math.Max(leftMin, rightMin);

                if (prominence > prominenceThresholddB)
                {
                    candidates.Add((
                        peak.i,
                        spectrum.FrequenciesHz[peak.i],
                        peakVal)); // store power in dB
                }
            }

            var output = new List<(int i, double frequency)>();

            return candidates
                .OrderByDescending(x => x.powerDb)
                .Take(maxPeaks)
                .Select(x => (x.i, x.frequency))
                .ToList();
        }
    }

}
