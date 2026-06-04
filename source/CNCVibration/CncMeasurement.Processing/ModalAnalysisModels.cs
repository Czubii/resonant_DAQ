using CncMeasurement.Core.models;

namespace CncMeasurement.Processing
{
    public sealed record ModalAnalysisReportInternal
    (
        ModalResultsInternal NumericalResults,
        FftFrame SignalFFT,
        SignalFrame SignalRaw
    );

    public sealed record ModalResultsInternal(
        long SampleIndex,
        DateTime TimeStampUtc,
        ModalModeInternal[] Modes
    );
    public sealed record ModalModeInternal(
        double FrequencyHz,
        ModalModeChannelInternal[] Channels
    );

    public sealed record ModalModeChannelInternal
    (
        string AssignedChannelName,
        double PsdAtMode,
        double FftMagnitudeAtMode,
        double DecayTime,
        double DampingRate,
        double DampingRegressionQuality,
        double[] Envelope

    );
    public static class ModalMapping
    {
        public static ModalAnalysisReport ToPublic(this ModalAnalysisReportInternal src)
        {
            return new ModalAnalysisReport(
                src.NumericalResults.ToPublic(),
                src.SignalFFT,
                src.SignalRaw
            );
        }
        public static ModalResults ToPublic(this ModalResultsInternal src)
        {
            return new ModalResults(
                src.SampleIndex,
                src.TimeStampUtc,
                src.Modes.Select(m => m.ToPublic()).ToArray()
            );
        }

        public static ModalMode ToPublic(this ModalModeInternal src)
        {
            return new ModalMode(
                src.FrequencyHz,
                src.Channels.Select(c => c.ToPublic()).ToArray()
            );
        }

        public static ModalModeChannel ToPublic(this ModalModeChannelInternal src)
        {
            return new ModalModeChannel(
                src.AssignedChannelName,
                src.PsdAtMode,
                src.FftMagnitudeAtMode,
                src.DecayTime,
                src.DampingRate,
                src.DampingRegressionQuality
            );
        }
    }
}
