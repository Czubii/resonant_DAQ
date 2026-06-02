using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Processing
{

    public sealed record ModalResults(
        long SampleIndex,
        DateTime TimeStampUtc,
        double SampleRate,
        ModalMode[] Modes
    );
    public sealed record ModalMode(
        double FrequencyHz,
        ModalChannelResult[] Channels
    );

    public sealed record ModalChannelResult
    (
        string ChannelName,
        double SpectrumAmplitude
    );
    public class ModalAnalyzer
    {
        public ModalResults Analyze(SignalFrame signalWindow, FftFrame fftSpectra)
        {
            int nChannels = signalWindow.Channels.Length;

            var resonances = FFTSpectrum.ResonantFrequencies(fftSpectra);
            var resonantFrequencies = resonances.Select(a => a.ResonantFrequencyHz).ToArray();
            var peakAmplitudes = resonances.Select(a=>a.PeakAmplitude).ToArray();

            var dampingRatios = DampingEstimator.Compute(signalWindow, resonantFrequencies, 5);


            for (int ch = 0; ch < nChannels; ch++)
            {
                Console.WriteLine($"Channel {signalWindow.Channels[ch].AssignedChannelName} | Damping Ratio: {dampingRatios[ch]}");
            } 

            return new ModalResults
            (
                signalWindow.SampleIndex,
                signalWindow.TimeStamp
            );
        }
    }
}
