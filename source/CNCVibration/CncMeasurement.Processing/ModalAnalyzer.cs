using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Processing
{

    public sealed record ModalResultsChannel
    (
        FftFrame Spectrum,

        double ResonantFrequencyHz,
        double PeakAmplitude,

        double DampingRatio,
        double LogarithmicDecrement,
        double DecayRate,

        double QualityFactor,

        double DampedNaturalFrequencyHz,

        double FitR2
    );
    public sealed record ModalResults
    (
        DateTime TimeStamp, //Start of the window
        ModalResultsChannel[] Channels
        // Maybe some additional information like mode corelation factor, etc

    );
    public class ModalAnalyzer
    {
        public ModalResults Analyze(SignalFrame signalWindow)
        {
            int nChannels = signalWindow.Channels.Length;
            var fftSpectra = FFTSpectrum.Compute(signalWindow);

            var resonantFrequenciees = fftSpectra.Channels.Select(a => a.ResonantFrequency).ToArray();
            var dampingRatios = DampingEstimator.Compute(signalWindow, resonantFrequenciees, 10);

            for (int ch = 0; ch < nChannels; ch++)
            {
                Console.WriteLine($"Channel {signalWindow.Channels[ch].AssignedChannelName} | Damping Ratio: {dampingRatios[ch]}");
            } 

            return new ModalResults { Spectrum = fftSpectrum };
        }
    }
}
