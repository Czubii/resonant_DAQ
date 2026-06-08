using CncMeasurement.Core.models;
using CncMeasurement.Core.Interfaces;
using System.Diagnostics.Contracts;
using System.Threading.Channels;
using System.Runtime.CompilerServices;
using CncMeasurement.Processing;

namespace CncMeasurement.Engine
{
    
    public class Engine : IEngine
    {

        IMachineController _machineController {  get; set; }
        IDatabaseController _databaseController { get; set; }
        //IMeasurementBroadcaster _broadcaster;

        IDataAcquisitionService _DAQ {  get; set; }

        IDaqDiscovery _discovery;

        IModalAnalysisService _analyzer { get; set; }

        IMeasurementBroadcaster _broadcaster;
        public Engine(IMachineController machinecontroller,
            IDatabaseController databaseController,
            IMeasurementBroadcaster broadcaster,
            IDataAcquisitionService daq, IModalAnalysisService analyzer)
        {
            _machineController = machinecontroller;
            _databaseController = databaseController;
            _DAQ = daq;
            _analyzer = analyzer;
            _broadcaster = broadcaster;
           
        }
        private List<ChannelReader<SampleChunk>> _outputReaders;
        ExperimentSetup _standardSetup;
        ModalAnalysisExperimentSetup _modalSetup;

        public Task ModalTask { get; private set; }

        public bool IsExperimentRunning { get; private set; } = false;


        /// measurement procedure:
        /// 1. Load the data to the machine.

        /// 2. Load sensor configuration.
        /// 3. Command the machine and sensors to begin the first step (RPM SWEEP)
        /// 4. Pass the rpm domain data to processing
        /// 5. Command the machine to run at the detected RPM[s]
        /// 6. pass the gathered time domain data to processing
        /// 7. Pass every bit of data to the database handler
        /// 8. send notification about the completion
        //public async Task RunExperiment(CancellationToken ct)
        //{
        //    if ( _standardSetup == null)
        //    {
        //        throw new Exception("Experiment setup not initialized");
        //    }
        //    int OutputCount = 3;

        //    //await _machineController.SetYPosition(_standardSetup.MachineConfiguration.Y);

        //    //_DAQ.StartAsync(_standardSetup.MeasurementConfig);

        //    //// broadcast the collected data live to the other services
        //    //var BroadcastingTask = BroadcastDataSweep(_DAQ.Reader, ct);

        //    //await _machineController.RunSweep();
        //    //await _DAQ.StopAsync();
        //    //// message the sweep is complete

        //    //await BroadcastingTask;

        //    // Gather the peaks from the data

        //    // Run at the peak
        //    _databaseController.InitializeContext();

            
        //    _ = _DAQ.Start(_standardSetup.MeasurementConfig, ct);
        //    // _machineController.RunContinous(500);

            

        //    // broadcast the data to the different readers
        //    _outputReaders = Split(_DAQ.Reader, OutputCount);
        //    _ = _processor.Start(_outputReaders[0], ct);
        //    _ = StartSignalRStreaming(_outputReaders[1], ct);
        //    ChannelReader<RmsFrame> RMSreader = _processor.RMSReader;
        //    ChannelReader<FftFrame> FFTreader = _processor.FFTReader;

        //    _ = _databaseController.StartLogLiveExperiment(
        //        _standardSetup,
        //        RMSreader,
        //        FFTreader
        //        );
            

        //    await Task.Delay((int)(_standardSetup.DurationMS));
        //    await _DAQ.StopAsync();
        //    await _databaseController.StopLog();

            
        //}

        public string ModalStatus()
        {
            if (ModalTask.IsCompleted)
            {
                return $"Experiment completed. ID: {_modalSetup.ID}";
            }
            return null;
        }

        public async Task RunModalExperiment(CancellationToken ct)
        {
            ModalTask = InternalExperiment(ct);
        }

        async Task InternalExperiment(CancellationToken ct)
        {
            var triggerConfig = new TriggerConfig
            {
                SampleRate = _modalSetup.MeasurementConfig.SampleRate,
                ChannelConfigs = _modalSetup.MeasurementConfig.ChannelConfigs,
                PreTriggerWindowMs = 250,
                PostTriggerWindowMs = 250,
                Threshold = 1.0
            };
            var analysisConfig = new ModalAnalysisConfig
            {
                ModeProminenceThresholddB = 2,
                DampingFilterBandwidthPercent = 0.1,
                DampingStartPeakPercent = 0.95f,
                DampingEndPeakPercent = 0.15f,
                UseNDominantModes = 8
            };
            var report = await _analyzer.RunAsync(_modalSetup.MeasurementConfig, triggerConfig, analysisConfig, ct);
            Console.WriteLine("[Engine] Recieved analysis from processor");

            ModalExperimentSchema schema = new();
            schema.FromSetup(_modalSetup, report);

            _databaseController.SaveModalExperimentSchema(schema);
            Console.WriteLine($"[Engine] Experiment {schema.ID} complete");
        }
        

        private async Task StartSignalRStreaming(ChannelReader<SampleChunk> reader, CancellationToken ct)
        {
            try
            {
                await foreach (var chunk in reader.ReadAllAsync(ct))
                {
                    await _broadcaster.BroadcastMeasurementAsync(chunk, ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
        private static List<ChannelReader<SampleChunk>> Split(ChannelReader<SampleChunk> sourceReader, int outputCount)
        {
            var outputs = new Channel<SampleChunk>[outputCount];

            for (int i = 0; i < outputCount; i++)
            {
                outputs[i] = Channel.CreateUnbounded<SampleChunk>();
            }

            // Fire and forget the background worker that routes the traffic
            _ = Task.Run(() => BroadcastLoopAsync(sourceReader, outputs));

            // Return just the readers so your functions can consume them safely
            return outputs.Select(ch => ch.Reader).ToList();
        }

        private static async Task BroadcastLoopAsync(ChannelReader<SampleChunk> source, Channel<SampleChunk>[] outputs)
        {
            try
            {
                // Read until the source channel is marked as complete
                await foreach (var chunk in source.ReadAllAsync())
                {
                    Console.WriteLine(chunk.TimeStamp);
                    Console.WriteLine(chunk.Samples);
                    foreach (var output in outputs)
                    {
                        // Write the exact same chunk reference to all subscribers
                        await output.Writer.WriteAsync(chunk);
                    }
                }
            }
            finally
            {
                // If the source channel finishes (or crashes), gracefully close the output channels
                foreach (var output in outputs)
                {
                    output.Writer.Complete();
                }
            }
        }
        //private async Task BroadcastDataSweep(ChannelReader<SampleChunk> reader, CancellationToken ct)
        //{
        //    await foreach (var data in reader.ReadAllAsync(ct))
        //    {
        //        _processor.QueueForProcessingAsync(data, ct);
        //        _databaseController.QueueForSavingAsync(data, ct);
        //        _broadcaster.BroadcastMeasurementAsync(data, ct);
        //    }
        //}
        public Task LoadExperiment(ExperimentSetup Setup)
        {
            _standardSetup = Setup;

            // _ = await _machineController.SetYPosition(Setup.MachineConfiguration.Y);
            
            return Task.CompletedTask;
        }
        public async Task LoadExperiment(ModalAnalysisExperimentSetup Setup)
        {
            Console.WriteLine($"[Engine] Loading experiment: {Setup.ID}");
            _modalSetup = Setup;
            // _ = await _machineController.SetYPosition(_modalSetup.MachineConfig.Y);
            Console.WriteLine($"[Engine] Experiment loaded: {Setup.ID}. Machine ready to start measurements");
            return;
        }
    }
}
