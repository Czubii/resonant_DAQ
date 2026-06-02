using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Processing
{

    public sealed record ModalChannelObservation
    (
        string AssignedChannelName,

        double AmplitudeAtMode,
        double PhaseAtMode,        // optional but very valuable

        double DampingContribution // optional (usually shared anyway)
    );
    public sealed record ModalResults
    (
        long SampleIndex,
        DateTime TimeStamp,
        ModalResultsChannel[] Channels
    );
    public class ModalAnalyzer
    {
        public ModalResults Analyze(SignalFrame signalWindow, FftFrame fftSpectra)
        {
            int nChannels = signalWindow.Channels.Length;

            var resonances = FFTSpectrum.ResonantFrequencies(fftSpectra);
            var resonantFrequencies = resonances.Select(a => a.ResonantFrequencyHz).ToArray();
            var peakAmplitudes = resonances.Select(a=>a.PeakAmplitude).ToArray();

            var dampingRatios = DampingEstimator.Compute(signalWindow, resonantFrequencies, 10);

            var resultsChannels = new ModalResultsChannel[nChannels];

            for (int ch = 0; ch < nChannels; ch++)
            {
                resultsChannels[ch] = new ModalResultsChannel
                (
                    signalWindow.Channels[ch].AssignedChannelName,

                    resonantFrequencies[ch],
                    peakAmplitudes[ch],

                    dampingRatios[ch]

                );
                Console.WriteLine($"Channel {signalWindow.Channels[ch].AssignedChannelName} | Damping Ratio: {dampingRatios[ch]}");
            } 

            return new ModalResults
            (
                signalWindow.SampleIndex,
                signalWindow.TimeStamp,
                resultsChannels
            );
        }
    }
}
