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

    public interface IModalAnalyzer
    {
        public ModalResultsInternal Analyze(SignalFrame rawSignalWindow, FftFrame fftSpectra, ModalAnalysisConfig config, ReportDetailLevel level);
    }
    public class ModalAnalyzer: IModalAnalyzer
    {
        public ModalResultsInternal Analyze(SignalFrame rawSignalWindow, FftFrame fftFrame, ModalAnalysisConfig config, ReportDetailLevel level)
        {

            int nChannels = rawSignalWindow.Channels.Length;

            // each prominent peak is one of the modes of the structure
            var prominentPeaks = FFTSpectrumTools.DetectCombinedSpectrumPeaks(fftFrame, config.ModeProminenceThresholddB, config.UseNDominantModes);
            int nPeaks = prominentPeaks.Count;

            ModalModeInternal[] modes = new ModalModeInternal[prominentPeaks.Count];

            for (int i = 0; i < nPeaks; i++) 
            {
                var modeChannelResults = new ModalModeChannelInternal[nChannels];
                var peakIdx = prominentPeaks[i].i;
                var peakFrequ = prominentPeaks[i].frequency; // frequency at which magnitude peaks

                var bandwidth = config.DampingFilterBandwidthPercent * peakFrequ;

                for (int ch = 0; ch < nChannels; ch++)
                {
                    var envelope = ModeEnvelope.Extract(
                        rawSignalWindow.Channels[ch].Samples, rawSignalWindow.SampleRateHz, peakFrequ, bandwidth);
                    var damping = WaveformTools.EstimateModeDamping(
                        envelope, rawSignalWindow.SampleRateHz, peakFrequ, config.DampingStartPeakPercent, config.DampingEndPeakPercent);

                    modeChannelResults[ch] = new ModalModeChannelInternal
                    (
                        rawSignalWindow.Channels[ch].AssignedChannelName,
                        fftFrame.Channels[ch].PSDMagnitudes[peakIdx],
                        fftFrame.Channels[ch].Magnitudes[peakIdx],
                        damping.DecayTime,
                        damping.DampingRatio,
                        damping.R2,
                        level == ReportDetailLevel.Full ? envelope : null,
                        level == ReportDetailLevel.Full ? 
                        FFTBandPass.Apply(rawSignalWindow.Channels[ch].Samples, rawSignalWindow.SampleRateHz, peakFrequ, bandwidth) 
                        : null
                    );
                }

                modes[i] = new ModalModeInternal
                (
                    peakFrequ,
                    modeChannelResults
                );
            }

            return new ModalResultsInternal
            (
                rawSignalWindow.SampleIndex,
                rawSignalWindow.TimeStamp,
                modes
            );

        }
    }
}
