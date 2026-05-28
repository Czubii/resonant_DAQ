using CncMeasurement.Core.models;
using CncMeasurement.Core.Interfaces;
using CncMeasurement.Data;
using CncMeasurement.Machine;
using CncMeasurement.Processing;
using System.Diagnostics.Contracts;
using System.Threading.Channels;

namespace CncMeasurement.Engine
{
    
    public class Engine : IEngine
    {

        IMachineController _machineController;
        IDatabaseController _databaseController;
        IProcessing _processor;
        IMeasurementBroadcaster _broadcaster;
        IDataAcquisitionService _DAQ;
        public Engine(IMachineController machinecontroller, IDatabaseController databaseController, IProcessing processor, IMeasurementBroadcaster broadcaster)
        {
            _machineController = machinecontroller;
            _databaseController = databaseController;
            _processor = processor;
            _broadcaster = broadcaster;
        }

        ExperimentSetup _setup;
        /// measurement procedure:
        /// 1. Load the data to the machine.

        /// 2. Load sensor configuration.
        /// 3. Command the machine and sensors to begin the first step (RPM SWEEP)
        /// 4. Pass the rpm domain data to processing
        /// 5. Command the machine to run at the detected RPM[s]
        /// 6. pass the gathered time domain data to processing
        /// 7. Pass every bit of data to the database handler
        /// 8. send notification about the completion
        public async Task RunExperiment(CancellationToken ct)
        {

            //await _machineController.SetYPosition(_setup.MachineConfiguration.Y);

            //_DAQ.StartAsync(_setup.MeasurementConfig);

            //// broadcast the collected data live to the other services
            //var BroadcastingTask = BroadcastDataSweep(_DAQ.Reader, ct);

            //await _machineController.RunSweep();
            //await _DAQ.StopAsync();
            //// message the sweep is complete

            //await BroadcastingTask;

            // Gather the peaks from the data

            // Run at the peak

            _DAQ.StartAsync(_setup.MeasurementConfig);
            _machineController.RunContinous(500);
            var BroadcastingTask = BroadcastData(_DAQ.Reader, ct);
            

            await Task.Delay((int)(_setup.MeasurementConfig.DurationSeconds*1000));
            await _machineController.Stop();
            await _DAQ.StopAsync();
            await BroadcastingTask;
        }
        private async Task BroadcastData(ChannelReader<SampleChunk> reader, CancellationToken ct)
        {
            await foreach (var data in reader.ReadAllAsync(ct))
            {
                _processor.QueueForProcessingAsync(data, ct);
                _databaseController.QueueForSavingAsync(data, ct);
                _broadcaster.BroadcastMeasurementAsync(data, ct);
            }
        }
        private async Task BroadcastDataSweep(ChannelReader<SampleChunk> reader, CancellationToken ct)
        {
            await foreach (var data in reader.ReadAllAsync(ct))
            {
                _processor.QueueForProcessingAsync(data, ct);
                _databaseController.QueueForSavingAsync(data, ct);
                _broadcaster.BroadcastMeasurementAsync(data, ct);
            }
        }
        public Task LoadExperiment(ExperimentSetup Setup)
        {

            _machineController.SetYPosition(Setup.MachineConfiguration.Y);
            
            return Task.CompletedTask;
        }

        void IEngine.LoadExperiment(ExperimentSetup Setup)
        {
            throw new NotImplementedException();
        }
    }
}
