using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace CncMeasurement.Processing
{
    public interface IModalExcelReportBuilder
    {
        Task<string> BuildAsync(
            ModalAnalysisReportInternal report,
            string outputPath,
            CancellationToken ct);
    }
    public sealed class ModalExcelReportBuilder : IModalExcelReportBuilder
    {
        public Task<string> BuildAsync(
            ModalAnalysisReportInternal report,
            string outputPath,
            CancellationToken ct)
        {
            using var wb = new XLWorkbook();

            BuildSummarySheet(wb, report);
            BuildModalSheet(wb, report.NumericalResults);
            BuildFftSheet(wb, report.SignalFFT);
            BuildPsdSheet(wb, report.SignalFFT);

            BuildEnvelopeSheet(wb, report);
            BuildRawSignalSheet(wb, report.SignalRaw);

            BuildConfigurationSheet(wb, report.AcquisitionConfig, report.TriggerConfig, report.AnalysisConfig);

            wb.SaveAs(outputPath);

            return Task.FromResult(outputPath);
        }
        private static void BuildSummarySheet(XLWorkbook wb, ModalAnalysisReportInternal report)
        {
            var ws = wb.Worksheets.Add("Summary");

            ws.Cell(1, 1).Value = "Modal Analysis Report";
            ws.Cell(1, 1).Style.Font.Bold = true;

            ws.Cell(3, 1).Value = "Sample Index";
            ws.Cell(3, 2).Value = report.NumericalResults.SampleIndex;

            ws.Cell(4, 1).Value = "Timestamp (UTC)";
            ws.Cell(4, 2).Value = report.NumericalResults.TimeStampUtc;

            ws.Cell(6, 1).Value = "Modes Count";
            ws.Cell(6, 2).Value = report.NumericalResults.Modes.Length;

            ws.Columns().AdjustToContents();
        }
        private static void BuildModalSheet(XLWorkbook wb, ModalResultsInternal results)
        {
            var ws = wb.Worksheets.Add("Modes");

            ws.Cell(1, 1).Value = "Mode Index";
            ws.Cell(1, 2).Value = "Frequency [Hz]";
            ws.Cell(1, 3).Value = "Channel";
            ws.Cell(1, 4).Value = "PSD at Mode";
            ws.Cell(1, 5).Value = "FFT Magnitude";
            ws.Cell(1, 6).Value = "Decay Time [s]";
            ws.Cell(1, 7).Value = "Damping Rate";
            ws.Cell(1, 8).Value = "Damping Rate R^2";

            ws.Range(1, 1, 1, 8).Style.Font.Bold = true;

            int row = 2;

            for (int i = 0; i < results.Modes.Length; i++)
            {
                var mode = results.Modes[i];

                foreach (var ch in mode.Channels)
                {
                    ws.Cell(row, 1).Value = i;
                    ws.Cell(row, 2).Value = mode.FrequencyHz;
                    ws.Cell(row, 3).Value = ch.AssignedChannelName;
                    ws.Cell(row, 4).Value = ch.PsdAtMode;
                    ws.Cell(row, 5).Value = ch.FftMagnitudeAtMode;
                    ws.Cell(row, 6).Value = ch.DampingRate;
                    ws.Cell(row, 7).Value = ch.DecayTime;
                    ws.Cell(row, 8).Value = ch.DampingRegressionQuality;

                    row++;
                }
            }

            ws.Columns().AdjustToContents();
        }
        private static void BuildEnvelopeSheet(XLWorkbook wb, ModalAnalysisReportInternal results)
        {
            bool hasEnvelope = results.NumericalResults.Modes
                .SelectMany(m => m.Channels)
                .Any(c => c.Envelope != null);

            bool hasSignal = results.NumericalResults.Modes
                .SelectMany(m => m.Channels)
                .Any(c => c.ModeTimeSignal != null);

            // If nothing to export → do not create sheet
            if (!hasEnvelope && !hasSignal)
                return;

            var ws = wb.Worksheets.Add("Filtered Modes");

            ws.Cell(1, 1).Value = "Mode Frequency [Hz]";
            ws.Cell(1, 2).Value = "Channel";
            ws.Cell(1, 3).Value = "Time [s]";
            ws.Cell(1, 4).Value = "Envelope";
            ws.Cell(1, 5).Value = "Mode Signal";

            int row = 2;
            var sampleRate = results.SignalRaw.SampleRateHz;

            foreach (var mode in results.NumericalResults.Modes)
            {
                foreach (var channel in mode.Channels)
                {
                    int maxLen =
                        Math.Max(
                            channel.Envelope?.Length ?? 0,
                            channel.ModeTimeSignal?.Length ?? 0);

                    if (maxLen == 0)
                        continue;

                    for (int i = 0; i < maxLen; i++)
                    {
                        ws.Cell(row, 1).Value = mode.FrequencyHz;
                        ws.Cell(row, 2).Value = channel.AssignedChannelName;
                        ws.Cell(row, 3).Value = i / sampleRate;

                        if (channel.Envelope != null && i < channel.Envelope.Length)
                            ws.Cell(row, 4).Value = channel.Envelope[i];

                        if (channel.ModeTimeSignal != null && i < channel.ModeTimeSignal.Length)
                            ws.Cell(row, 5).Value = channel.ModeTimeSignal[i];

                        row++;
                    }
                }
            }

            var table = ws.Range(1, 1, row - 1, 5)
                .CreateTable();

            table.Name = "ModalSignalTable";
            table.Theme = XLTableTheme.TableStyleMedium6;

            ws.Columns().AdjustToContents();
        }
        private static void BuildFftSheet(XLWorkbook wb, FftFrame fft)
        {
            var ws = wb.Worksheets.Add("FFT");

            ws.Cell(1, 1).Value = "Frequency [Hz]";
            ws.Cell(1, 1).Style.Font.Bold = true;

            for (int ch = 0; ch < fft.Channels.Length; ch++)
            {
                ws.Cell(1, ch + 2).Value = fft.Channels[ch].AssignedChannelName;
            }

            int rows = fft.FrequenciesHz.Length;

            for (int i = 0; i < rows; i++)
            {
                ws.Cell(i + 2, 1).Value = fft.FrequenciesHz[i];

                for (int ch = 0; ch < fft.Channels.Length; ch++)
                {
                    ws.Cell(i + 2, ch + 2).Value = fft.Channels[ch].Magnitudes[i];
                }
            }

            ws.Columns().AdjustToContents();
        }
        private static void BuildPsdSheet(XLWorkbook wb, FftFrame fft)
        {
            var ws = wb.Worksheets.Add("PSD");

            ws.Cell(1, 1).Value = "Frequency [Hz]";
            ws.Cell(1, 1).Style.Font.Bold = true;

            for (int ch = 0; ch < fft.Channels.Length; ch++)
            {
                ws.Cell(1, ch + 2).Value = fft.Channels[ch].AssignedChannelName;
            }

            int rows = fft.FrequenciesHz.Length;

            for (int i = 0; i < rows; i++)
            {
                ws.Cell(i + 2, 1).Value = fft.FrequenciesHz[i];

                for (int ch = 0; ch < fft.Channels.Length; ch++)
                {
                    ws.Cell(i + 2, ch + 2).Value = fft.Channels[ch].PSDMagnitudes[i];
                }
            }

            ws.Columns().AdjustToContents();
        }
        private static void BuildRawSignalSheet(XLWorkbook wb, SignalFrame signal)
        {
            var ws = wb.Worksheets.Add("RawSignal");

            ws.Cell(1, 1).Value = "Time [s]";
            ws.Cell(1, 1).Style.Font.Bold = true;

            for (int ch = 0; ch < signal.Channels.Length; ch++)
            {
                ws.Cell(1, ch + 2).Value = signal.Channels[ch].AssignedChannelName;
            }

            int samples = signal.Channels[0].Samples.Length;
            var sampleRate = signal.SampleRateHz;

            for (int i = 0; i < samples; i++)
            {
                ws.Cell(i + 2, 1).Value = i/sampleRate;

                for (int ch = 0; ch < signal.Channels.Length; ch++)
                {
                    ws.Cell(i + 2, ch + 2).Value = signal.Channels[ch].Samples[i];
                }
            }

            ws.Columns().AdjustToContents();
        }

        private static void BuildConfigurationSheet(
            XLWorkbook wb,
            AcquisitionConfig acq,
            TriggerConfig trig,
            ModalAnalysisConfig analysis)
        {
            var ws = wb.Worksheets.Add("Configuration");

            int row = 1;

            ws.Cell(row++, 1).Value = "ACQUISITION CONFIG";
            ws.Cell(row++, 1).Value = $"Sample Rate: {acq.SampleRate}";
            ws.Cell(row++, 1).Value = $"Chunk Size: {acq.ChunkSize}";
            ws.Cell(row++, 1).Value = $"Group Name: {acq.GroupName}";
            ws.Cell(row++, 1).Value = $"Output Path: {acq.OutputTDMSPath}";

            row++;

            ws.Cell(row++, 1).Value = "CHANNEL CONFIGS";

            foreach (var ch in acq.ChannelConfigs)
            {
                ws.Cell(row++, 1).Value =
                    $"{ch.NameToAssignToChannel} | {ch.PhysicalChannelName} | Range: {ch.MinRange}..{ch.MaxRange} | Sensitivity: {ch.Sensitivity}";
            }

            row++;

            ws.Cell(row++, 1).Value = "TRIGGER CONFIG";
            ws.Cell(row++, 1).Value = $"Pre-trigger (ms): {trig.PreTriggerWindowMs}";
            ws.Cell(row++, 1).Value = $"Post-trigger (ms): {trig.PostTriggerWindowMs}";
            ws.Cell(row++, 1).Value = $"Threshold: {trig.Threshold}";

            row++;

            ws.Cell(row++, 1).Value = "ANALYSIS CONFIG";
            ws.Cell(row++, 1).Value = $"Prominence (dB): {analysis.ModeProminenceThresholddB}";
            ws.Cell(row++, 1).Value = $"Damping Bandwidth (%): {analysis.DampingFilterBandwidthPercent}";
            ws.Cell(row++, 1).Value = $"Start Peak %: {analysis.DampingStartPeakPercent}";
            ws.Cell(row++, 1).Value = $"End Peak %: {analysis.DampingEndPeakPercent}";
            ws.Cell(row++, 1).Value = $"N Modes: {analysis.UseNDominantModes}";

            ws.Columns().AdjustToContents();
        }
    }
}
