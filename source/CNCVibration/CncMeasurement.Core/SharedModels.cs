using System;
using System.Text.Json.Serialization;

namespace CncMeasurement.Core.models
{


    // general information about the database collections
    public class DBinfo
    {
        public string[] Collections;
    }
    // General measurement data
    public class MeasurementMetadata
    {
        public int ID { get; set; }
        public DateTime Timestamp { get; set; }
        public GraphMetadata[] Graphs { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }
    }
    // for storing the informations about a .TDMS graph
    public class GraphMetadata
    {

        public string Description { get; set; }
        public string Xaxis { get; set; }
        public string Yaxis { get; set; }
        public string FilePath { get; set; }
    }
    public class BriefMeasurementInfo
    {
        public int ID { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Basic device information that allow for channel selection
    /// </summary>
    public class DeviceDescription
    {
        public string DeviceName { get; set; }
        public List<string> AIChannels { get; set; }
        // This entry contains analog input channels of the device in the following form:
        //  cDAQ1Mod1/ai0
        //  cDAQ1Mod1/ai1
        //  cDAQ1Mod1/ai2
        //  cDAQ1Mod1/ai3
        // If no analog input channels are present for the device this list will be just empty

        public string ProductType { get; set; }
        public string SerialNumber { get; set; }
    }

    public class MachineConfig
    {
        public int Y { get; set; }
    }

    public class ExperimentSetup
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public MachineConfig MachineConfiguration { get; set; }

        public AcquisitionConfig MeasurementConfig { get; set; }
        public int DurationMS { get; set; }
    }
    public class ChannelConfig
    {
        public string PhysicalChannelName { get; set; } //= "cDAQ1Mod1/ai0";
        public string NameToAssignToChannel { get; set; } // for example "Sensor Frame" or something idk
        public float Sensitivity { get; set; }
        public float MinRange { get; set; }
        public float MaxRange { get; set; }
    }
    public class AcquisitionConfig
    {
        public string GroupName;
        public string OutputTDMSPath;
        public List<ChannelConfig> ChannelConfigs { get; set; }
        public float SampleRate { get; set; } //= 10240.0;
        public int ChunkSize { get; set; } // optimal value will depend on sample rate. For 10kS/s 4096 should be an okay starting value
    }
    public class TriggerConfig
    {
        public double SampleRate { get; set; }
        public List<ChannelConfig> ChannelConfigs { get; set; }
        public int PreTriggerWindowMs { get; set; }
        public int PostTriggerWindowMs { get; set; }

        public double Threshold { get; set; }
    }
    public class ModalAnalysisConfig
    {
        public double ModeProminenceThresholddB { get; set; }
        public double DampingFilterBandwidthPercent { get; set; }
        public float DampingStartPeakPercent { get; set; }
        public float DampingEndPeakPercent { get; set; }
        public int UseNDominantModes { get; set; }
    }
    public sealed record BroacastFrame
    {
        List<SampleChunk> samples;
        List<RmsFrame> RmsFrames;
        List<FftFrame> FftFrames;

    }
    public sealed record SampleChunk // Used for sample transport between daq an windowing/trigger layers
        (
            long SampleIndex,
            int NumChannels,
            string[] AssignedChannelNames,
            int NumSamples,
            double SampleRate,
            DateTime TimeStamp, //Start of the Chunk
            double[,] Samples
        );

    public sealed record SignalFrame // Used for measurement transport between windowing/trigger layers and processing
    (
        long SampleIndex,
        double SampleRateHz,
        DateTime TimeStamp, //Start of the window
        SignalChannel[] Channels
    );
    public sealed record SignalChannel
    (
       string AssignedChannelName,
       double[] Samples
    );

    public sealed record FftFrame
    (
        long SampleIndex,
        int FFTSize,
        double[] FrequenciesHz,
        DateTime TimeStamp, //Start of the window
        FftChannel[] Channels
    );
    public sealed record FftChannel
    (
        string AssignedChannelName,
        double[] Magnitudes, // Magnitudes here approximate peak amplitude of sinusoidal component at frequency k

        // Computation:
        // - The time-domain signal x[n] (length = nRaw samples) is multiplied by a Hann window w[n].
        // - The windowed data is zero-padded to NFFT = FFTSize.
        // - A complex FFT is computed; X[k] is the FFT result.
        // - Magnitudes are computed as |X[k]| and scaled by:
        //      (1 / sum(w)) * S(k)
        //   where S(k) is the single-sided correction:
        //      S(0) = 1 (DC),
        //      S(NFFT/2) = 1 (Nyquist, if present),
        //      S(k) = 2 otherwise.
        //
        // Interpretation:
        // - Units match the time-domain input samples (e.g., m/s^2 for acceleration).
        // - For a bin-centered sinusoid, this scaling approximates the sinusoid’s PEAK amplitude.
        // - Not a density: values are not "per Hz" and depend on window/scaling choices.

        double[] PSDMagnitudes
    // Computation:
    // - Uses the same FFT X[k] as above.
    // - Window power normalization uses sum(w^2).
    // - PSD is computed as:
    //      PSD[k] = (|X[k]|^2 / (SampleRate * sum(w^2))) * S(k)
    //   with the same single-sided correction S(k) as for Magnitudes
    //   (DC and Nyquist are not doubled; interior bins are doubled).
    //
    // Interpretation:
    // - Units are (input units)^2/Hz.
    // - PSD is the preferred quantity for comparing spectral levels across different
    //   record lengths / FFT sizes and for averaging across repeated impacts or time windows.
    );
    public sealed record RmsFrame
    (
        long SampleIndex,
        DateTime Timestamp, //Start of the window
        RmsChannel[] Channels
    );
    public sealed record RmsChannel
    (
        string AssignedChannelName,
        double Value
    );

    public sealed record ModalResults(
    long SampleIndex,
    DateTime TimeStampUtc,
    ModalMode[] Modes
    );

    public sealed record ModalMode(
        double FrequencyHz,
        ModalModeChannel[] Channels
    );

    public sealed record ModalModeChannel(
        string AssignedChannelName,
        double PsdAtMode,
        double FftMagnitudeAtMode,
        double DecayTime,
        double DampingRate,
        double DampingRegressionQuality
    );

    /// <summary>
    /// Report that will be sent to client api for display
    /// More detailed information will be stored in Excel readable format
    /// </summary>
    public sealed record ModalAnalysisReport
    (
        ModalResults NumericalResults,
        FftFrame SignalFFT,
        SignalFrame SignalRaw,
        string ExcelReportPath
    );
    public class ExperimentSchema : ExperimentSetup
    {
        public List<DateTime> PeakStamps { get; set; }
        public List<string> FFTpaths { get; set; }
        public List<string> RMSpaths { get; set; }
    }

    public class ModalExperimentSchema : ModalAnalysisExperimentSetup
    {
        public ModalAnalysisReport Report { get; set; }

        public void FromSetup(ModalAnalysisExperimentSetup setup , ModalAnalysisReport report)
        {
            ID = setup.ID;
            Name = setup.Name;
            Description = setup.Description;
            MachineConfig = setup.MachineConfig;
            MeasurementConfig = setup.MeasurementConfig;
            TriggerConfig = setup.TriggerConfig;
            AnalysisConfig = setup.AnalysisConfig;
            Report = report;
        }
    }

    // Lightweight summary used by the database listing API
    public class ModalExperimentSchemaSummary
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public MachineConfig MachineConfig { get; set; }
    }

    public class ModalAnalysisExperimentSetup 
    {
        public Guid ID { get; protected set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public MachineConfig MachineConfig { get; set; }
        public AcquisitionConfig MeasurementConfig { get; set; }
        public TriggerConfig TriggerConfig { get; set; }
        public ModalAnalysisConfig AnalysisConfig { get; set; }

        public ModalAnalysisExperimentSetup()
        {
            ID = Guid.NewGuid();
        }
    }

}




