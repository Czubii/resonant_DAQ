using CncMeasurement.Core.Interfaces;
using CncMeasurement.Core.models;
using DocumentFormat.OpenXml.Bibliography;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Processing
{
    /// <summary>
    /// Add dependencies and initialize via similar pattern:
    /// builder.Services.AddSingleton<ModalAnalysisService>();
    /// 
    /// builder.Services.AddSingleton<IDaqService, NiDaqService>();
    /// builder.Services.AddSingleton<IModalAnalyzer, ModalAnalyzer>();
    /// 
    /// </summary>
    public class ModalAnalysisService : IModalAnalysisService, IAsyncDisposable
    {
        private readonly IDataAcquisitionService _rawSignalAcquisition;
        private readonly ITriggerWindowCapture _triggerCapture;
        private readonly IModalAnalyzer _analyzer;
        private readonly IModalExcelReportBuilder _reportBuilder;

        private readonly SemaphoreSlim _runLock = new(1, 1);

        public ModalAnalysisService(
            IDataAcquisitionService daq, 
            IModalAnalyzer analyzer, 
            ITriggerWindowCapture triggerCapture,
            IModalExcelReportBuilder reportBuilder)
        {
            _rawSignalAcquisition = daq;
            _analyzer = analyzer;
            _triggerCapture = triggerCapture;
            _reportBuilder = reportBuilder;
        }

        public async Task<ModalAnalysisReport> RunAsync(
            AcquisitionConfig DaqConfig, TriggerConfig TrigConfig, 
            ModalAnalysisConfig AnalConfig, CancellationToken ct)
        {
            await _runLock.WaitAsync(ct);
            try
            {

                await _rawSignalAcquisition.Start(DaqConfig, ct); 

                // Try to acquire the signal, after 20s throw
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                using var combined = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
                SignalFrame rawFrame;
                try
                {
                    rawFrame = await _triggerCapture.SingleCapture(_rawSignalAcquisition.Reader, TrigConfig, combined.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    throw new TimeoutException("No signal detected within 20 seconds, ");
                }
                finally
                {
                    await _rawSignalAcquisition.StopAsync(); // we can stop the acquisition by now
                }

                // Signal processing
                var spectrum = FFTConverter.ComputeFrame(rawFrame);

                var analysisResults = _analyzer.Analyze(rawFrame, spectrum, AnalConfig, ReportDetailLevel.Full);

                var report = new ModalAnalysisReportInternal
                (
                    analysisResults,
                    spectrum,
                    rawFrame,
                    DaqConfig,
                    TrigConfig,
                    AnalConfig
                );
                string ReportPath = $"Reports/modal_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                await _reportBuilder.BuildAsync(report, ReportPath, ct);

                return report.ToPublic(ReportPath);
            }
            finally
            {
                _runLock.Release();
            }

        }

        public async ValueTask DisposeAsync()
        {
            await _rawSignalAcquisition.DisposeAsync();
            _runLock.Dispose();
        }
    }
}
