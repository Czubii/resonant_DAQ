using CncMeasurement.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CncMeasurement.Core.models;

namespace CncMeasurement.Processing
{

    public sealed record ModalAnalysisReport
    {

    }

    /// <summary>
    /// Add dependencies and initialize via similar pattern:
    /// builder.Services.AddSingleton<ModalAnalysisService>();
    /// 
    /// builder.Services.AddSingleton<IDaqService, NiDaqService>();
    /// builder.Services.AddSingleton<IModalAnalyzer, ModalAnalyzer>();
    /// 
    /// </summary>
    public class ModalAnalysisService : IAsyncDisposable
    {
        private readonly IDataAcquisitionService _rawSignalAcquisition;
        private readonly ITriggerWindowCapture _triggerCapture;
        private readonly IModalAnalyzer _analyzer;

        private readonly SemaphoreSlim _runLock = new(1, 1);

        public ModalAnalysisService(IDataAcquisitionService daq, IModalAnalyzer analyzer)
        {
            _rawSignalAcquisition = daq;
            _analyzer = analyzer;
        }

        public async Task<ModalAnalysisReport> RunAsync(AcquisitionConfig DaqConfig, TriggerConfig TrigConfig, CancellationToken ct)
        {
            await _runLock.WaitAsync(ct);
            try
            {

                await _rawSignalAcquisition.Start(DaqConfig, ct); 

                // Try to acquire the signal, after 20s throw
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                using var combined = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                try
                {
                    var rawFrame = await _triggerCapture.SingleCapture(_rawSignalAcquisition.Reader, TrigConfig, combined.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    throw new TimeoutException("No signal detected within 20 seconds.");
                }


                var acquisitionStopTask = _rawSignalAcquisition.StopAsync();

                

                await acquisitionStopTask;

            }
            finally
            {
                _runLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _rawSignalAcquisition.DisposeAsync();
        }
    }
}
