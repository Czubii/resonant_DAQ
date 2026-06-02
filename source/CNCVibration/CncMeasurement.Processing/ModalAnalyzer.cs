using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Processing
{

    public sealed record ModalResults(
        long SampleIndex,
        DateTime TimeStampUtc,
        ModalMode[] Modes
    );
    public sealed record ModalMode(
        double FrequencyHz,
        ModalChannelResult[] Channels
    );

    public sealed record ModalChannelResult
    (
        string AssignedChannelName,
        double PsdAtMode,
        double FftMagnitudeAtMode,
        double DampingRate,
        double DampingRegressionQuality

    );

    public class ModalAnalysisConfig
    {
        public double ModeProminenceThresholddB;
        public double DampingFilterBandwidthPercent;
        public int DampingSkipNAfterPeak;
    }
    public class ModalAnalyzer
    {
        public void Analyze(SignalFrame signalWindow, FftFrame fftSpectra)
        {
            

            int nChannels = signalWindow.Channels.Length;

            var config = new ModalAnalysisConfig
            {
                ModeProminenceThresholddB = 3,
                DampingFilterBandwidthPercent = 0.2,
                DampingSkipNAfterPeak = 5
            };

            // each prominent peak is one of the modes of the structure
            var prominentPeaks = FFTSpectrum.CombinedSpectrumPeaks(fftSpectra, config.ModeProminenceThresholddB);
            int nPeaks = prominentPeaks.Count;

            ModalMode[] modes = new ModalMode[prominentPeaks.Count];


            // TESITNG FILTERING -------------------

            var centerFrequ = prominentPeaks[0].frequency;

            var bandwidthh = config.DampingFilterBandwidthPercent * centerFrequ;
            var filtered = FFTBandPass.Apply(signalWindow.Channels[0].Samples, signalWindow.SampleRateHz, centerFrequ, bandwidthh);

            var envelopee = ModeEnvelope.Extract(signalWindow.Channels[0].Samples, signalWindow.SampleRateHz, centerFrequ, bandwidthh);

            SaveComparison(
                "modal_debug.csv",
                signalWindow.Channels[0].Samples,
                filtered,
                envelopee,
                signalWindow.SampleRateHz);

            // ----------------------------------


            for (int i = 0; i < nPeaks; i++) 
            {
                var modeChannelResults = new ModalChannelResult[nChannels];
                var peakIdx = prominentPeaks[i].i;
                var peakFrequ = prominentPeaks[i].frequency; // frequency at which magnitude peaks

                var bandwidth = config.DampingFilterBandwidthPercent * peakFrequ;

                for (int ch = 0; ch < nChannels; ch++)
                {
                    var envelope = ModeEnvelope.Extract(signalWindow.Channels[ch].Samples, signalWindow.SampleRateHz, peakFrequ, bandwidth);
                    var damping = ModeDamping.ComputeFromEnvelope(envelope, signalWindow.SampleRateHz, peakFrequ, config.DampingSkipNAfterPeak);

                    modeChannelResults[ch] = new ModalChannelResult
                    (
                        signalWindow.Channels[ch].AssignedChannelName,
                        fftSpectra.Channels[ch].PSDMagnitudes[peakIdx],
                        fftSpectra.Channels[ch].Magnitudes[peakIdx],
                        damping.DampingRatio,
                        damping.R2
                    );
                }

                modes[i] = new ModalMode
                (
                    peakFrequ,
                    modeChannelResults
                );
            }

            var results = new ModalResults
            (
                signalWindow.SampleIndex,
                signalWindow.TimeStamp,
                modes
            );

            LogToConsole(results);
        }

        public static void SaveComparison(
        string filePath,
        double[] original,
        double[] filtered,
        double[] envelope,
        double sampleRateHz)
        {
            if (original.Length != filtered.Length)
                throw new ArgumentException("Signal lengths must match.");

            double dt = 1.0 / sampleRateHz;

            var sb = new StringBuilder();

            // header
            sb.AppendLine("Time_s,Original,Filtered,Envelope");

            for (int i = 0; i < original.Length; i++)
            {
                double t = i * dt;

                sb.Append(t.ToString("G17"));
                sb.Append(",");
                sb.Append(original[i].ToString("G17"));
                sb.Append(",");
                sb.Append(filtered[i].ToString("G17"));
                sb.Append(",");
                sb.Append(envelope[i].ToString("G17"));
                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString());
        }

        public static void LogToConsole(ModalResults results, string? title = null)
        {
            if (!string.IsNullOrWhiteSpace(title))
                Console.WriteLine(title);

            Console.WriteLine($"Time (UTC): {results.TimeStampUtc:O}");
            Console.WriteLine($"SampleIndex: {results.SampleIndex}");
            Console.WriteLine($"Modes: {results.Modes?.Length ?? 0}");
            Console.WriteLine();

            if (results.Modes == null || results.Modes.Length == 0)
            {
                Console.WriteLine("No modes detected.");
                return;
            }

            // Determine channel column width from the data we actually have
            int channelWidth = Math.Max(
                12,
                results.Modes
                    .SelectMany(m => m.Channels ?? Array.Empty<ModalChannelResult>())
                    .Select(c => c.AssignedChannelName?.Length ?? 0)
                    .DefaultIfEmpty(0)
                    .Max()
            );

            string Pad(string s, int width) => (s ?? "").PadRight(width);

            Console.WriteLine(
                $"{"Mode (Hz)",10}  {Pad("Channel", channelWidth)}  {"PsdAtMode",14}  {"FftMagnitudeAtMode",18} {"ModeDamping",11} {"RegressionQuality", 17}"
            );
            Console.WriteLine(new string('-', 10 + 2 + channelWidth + 2 + 14 + 2 + 18 + 11 + 17));

            foreach (var mode in results.Modes.OrderBy(m => m.FrequencyHz))
            {
                var channels = mode.Channels ?? Array.Empty<ModalChannelResult>();
                if (channels.Length == 0)
                {
                    Console.WriteLine($"{mode.FrequencyHz,10:0.###}  {Pad("(no channels)", channelWidth)}");
                    continue;
                }

                for (int i = 0; i < channels.Length; i++)
                {
                    var ch = channels[i];

                    // Only print mode frequency on the first line of the mode block
                    string modeText = (i == 0)
                        ? mode.FrequencyHz.ToString("0.###", CultureInfo.InvariantCulture)
                        : "";

                    Console.WriteLine(
                        $"{modeText,10}  {Pad(ch.AssignedChannelName, channelWidth)}  {ch.PsdAtMode,14:G6}  {ch.FftMagnitudeAtMode,18:G6} {ch.DampingRate,11:G6} {ch.DampingRegressionQuality,17:G6}"
                    );
                }
            }

            Console.WriteLine();
        }
    }
}
