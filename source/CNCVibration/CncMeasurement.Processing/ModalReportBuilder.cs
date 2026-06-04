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
            ModalAnalysisReport report,
            string outputPath,
            CancellationToken ct);
    }

    public sealed class ModalExcelReportBuilder : IModalExcelReportBuilder
    {
        public Task<string> BuildAsync(
            ModalAnalysisReport report,
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

            wb.SaveAs(outputPath);

            return Task.FromResult(outputPath);
        }
        private static void BuildSummarySheet(XLWorkbook wb, ModalAnalysisReport report)
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
        private static void BuildModalSheet(XLWorkbook wb, ModalResults results)
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
        private static void BuildEnvelopeSheet(XLWorkbook wb, ModalAnalysisReport results)
        {
            var ws = wb.Worksheets.Add("Envelopes");

            ws.Cell(1, 1).Value = "Mode Frequency [Hz]";
            ws.Cell(1, 2).Value = "Channel";
            ws.Cell(1, 3).Value = "Time [s]";
            ws.Cell(1, 4).Value = "Envelope Value";

            int row = 2;
            var sampleRate = results.SignalRaw.SampleRateHz;

            foreach (var mode in results.NumericalResults.Modes)
            {
                foreach (var channel in mode.Channels)
                {
                    for (int i = 0; i < channel.Envelope.Length; i++)
                    {
                        ws.Cell(row, 1).Value = mode.FrequencyHz;
                        ws.Cell(row, 2).Value = channel.AssignedChannelName;
                        ws.Cell(row, 3).Value = i/sampleRate;
                        ws.Cell(row, 4).Value = channel.Envelope[i];

                        row++;
                    }
                }
            }

            var table = ws.Range(1, 1, row - 1, 4)
                .CreateTable();

            table.Name = "EnvelopeTable";
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
    }
}
