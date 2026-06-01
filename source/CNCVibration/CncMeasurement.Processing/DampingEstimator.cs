using CncMeasurement.Core.models;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Processing
{
    public class DampingEstimator
    {
        /// <summary>
        /// Estimates damping ratio of a free-decay response signal. Works by extracting the peaks from the waveform,
        /// skipping first few to remove the initial transient, and finally applying log transform and fitting a line 
        /// through the points
        /// </summary>
        /// <param name="window"></param>
        /// <param name="skipFirstN"></param>
        /// <returns></returns>
        public static double[] Compute(SignalWindow window, double dominantFrequency, int skipFirstN)
        {

            PrintPeaks(window, 0);
            int nChannels = window.NumChannels;
            double dt = 1.0 / window.SampleRate;

            var output = new double[nChannels];

            for (int ch = 0; ch < nChannels; ch++)
            {
                var envelope = AbsEnvelope(window.Samples[ch], dt);
                var usable = envelope.Skip(skipFirstN).ToList(); // skipping first n peaks

                if (usable.Count < 2)
                    throw new InvalidOperationException("Not enough peaks after skipping transient.");

                // log transform
                var x = envelope.Select(a => a.x).ToArray();
                var y = envelope.Select(a => Math.Log(a.y)).ToArray();

                // Linear regression 

                double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
                int n = x.Length;

                for (int i = 0; i < n; i++)
                {
                    sumX += x[i];
                    sumY += y[i];
                    sumXY += x[i] * y[i];
                    sumX2 += x[i] * x[i];
                }

                double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
                double alpha = -slope;
                double omegaN = 2.0 * Math.PI * dominantFrequency;
                double dampingRatio = alpha / omegaN;

                output[ch] = dampingRatio;
            }

            return output;
        }

        private static void PrintPeaks(SignalWindow window, int channelToInspect)
        {
            double dt = 1.0 / window.SampleRate;
            double[] samples = window.Samples[channelToInspect];

            var envelope = AbsEnvelope(samples, dt);

            if (envelope.Count == 0)
                throw new InvalidOperationException("No peaks detected in signal.");

            // PRINT PEAKS (for debugging / validation)
            Console.WriteLine("Detected peaks (time, amplitude):");
            foreach (var p in envelope)
            {
                Console.WriteLine($"{p.x:F6}s  |  {p.y:F6}");
            }
        }

        /// <summary>
        /// computes points describing the envelope of absolute value of function
        /// desribed by set of equaly spaced points
        /// </summary>
        /// <returns></returns>
        private static List<(double x, double y)> AbsEnvelope(double[] samples, double dt)
        {
            int nSamples = samples.Length;
            var output = new List<(double x, double y)>();


            for (int i = 1; i < nSamples - 1; i++)
            {
                double prev = Math.Abs(samples[i - 1]);
                double curr = Math.Abs(samples[i]);
                double next = Math.Abs(samples[i + 1]);

                if (curr <= prev || curr <= next)
                    continue; // continue if current sample is not a local maximum

                // ignore zero or numerical noise
                if (curr < 1e-12)
                    continue;

                output.Add(new (i * dt, curr));
            }

            return output;
        }
    }
}
