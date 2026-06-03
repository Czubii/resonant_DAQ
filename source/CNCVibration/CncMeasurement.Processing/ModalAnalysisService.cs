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

        public async Task<ModalAnalysisReport> RunAsync(AcquisitionConfig config,CancellationToken ct)
        {
            await _runLock.WaitAsync(ct);
            try
            {

                await _rawSignalAcquisition.Start(config, ct); // TODO REMEMBER TO STOP THIS MADNESS

                // start trigger pipeline
                var triggerTask = _triggerCapture.Start(
                    signalSource.Reader,
                    triggerConfig);

                // CSV writer task (consumes SignalWindow stream)
                var csvTask = Task.Run(async () =>
                {
                    int counter = 0;

                    await foreach (var window in singleShotTrigger.Reader.ReadAllAsync(cts.Token))
                    {
                        var analyzer = new ModalAnalyzer();
                        var spectrum = FFTProcessor.ComputeFrame(window);
                        analyzer.Analyze(window, spectrum);

                        string rawPath = $"raw_{counter}.csv";
                        string fftPath = $"fft_{counter}.csv";

                        // 1. save raw signal
                        await WriteRawCsv(rawPath, window);

                        // 2. save FFT
                        await WriteFftCsv(fftPath, spectrum);

                        counter++;
                    }
                }, cts.Token);

                await triggerTask;
                await csvTask;

                var signal = await _rawSignalAcquisition.AcquireAsync(ct);
                return _analyzer.Analyze(signal);
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
