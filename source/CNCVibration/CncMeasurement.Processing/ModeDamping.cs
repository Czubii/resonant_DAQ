using CncMeasurement.Core.models;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Processing
{
    public class ModeDamping
    {
        public static (double R2, double DampingRatio) ComputeFromEnvelope(double[] envelope, double sampleRateHz, double modeFrequencyHz, int skipFirstN)
        {
            int nSamples = envelope.Length;

            double envelopePeakVal = double.MinValue;
            int envelopePeakIdx = 0;

            for (int i = 0; i < nSamples; i++)
            {
                if (envelope[i] > envelopePeakVal)
                {
                    envelopePeakVal = envelope[i];
                    envelopePeakIdx = i;
                }
            }

            int startIdx = envelopePeakIdx + skipFirstN;

            if (startIdx >= nSamples - 2) return (0.0, 0.0);
                //throw new ArgumentException("Not enough decay samples.");

            // linear regression accumulators
            double sumT = 0.0;
            double sumY = 0.0;
            double sumTT = 0.0;
            double sumTY = 0.0;

            int count = 0;

            // store values for R2
            List<double> tList = new();
            List<double> yList = new();

            for (int i = startIdx; i < nSamples; i++)
            {
                double a = envelope[i];

                if (a <= 0.0)
                    continue;

                double t = i / sampleRateHz;
                double y = Math.Log(a);

                tList.Add(t);
                yList.Add(y);

                sumT += t;
                sumY += y;
                sumTT += t * t;
                sumTY += t * y;

                count++;
            }

            if (count < 2)
                throw new ArgumentException("Not enough valid samples.");

            double slope =
                (count * sumTY - sumT * sumY) /
                (count * sumTT - sumT * sumT);

            double intercept =
                (sumY - slope * sumT) / count;

            double omegaN = 2.0 * Math.PI * modeFrequencyHz;
            double dampingRatio = -slope / omegaN;

            // mean of y
            double meanY = sumY / count;

            double ssTot = 0.0;
            double ssRes = 0.0;

            for (int i = 0; i < count; i++)
            {
                double y = yList[i];
                double t = tList[i];

                double yHat = intercept + slope * t;

                ssRes += (y - yHat) * (y - yHat);
                ssTot += (y - meanY) * (y - meanY);
            }

            double r2 = (ssTot <= 0.0) ? 0.0 : (1.0 - ssRes / ssTot);

            return (r2, dampingRatio);
        }
    }
}
