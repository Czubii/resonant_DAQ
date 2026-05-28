using CncMeasurement.Core.models;
using CncMeasurement.Data;
using CncMeasurement.Machine;
using CncMeasurement.Processing;

namespace CncMeasurement.Engine
{
    public interface IEngine
    {
        void LoadExperiment(ExperimentSetup Setup);
        void BeginExperiment();
    }
    public class Engine : IEngine
    {

        IMachineController _machineController;
        IDatabaseController _databaseController;
        IProcessing _processor;
        public Engine(IMachineController machinecontroller, IDatabaseController databaseController, IProcessing processor)
        {
            _machineController = machinecontroller;
            _databaseController = databaseController;
            _processor = processor;
        }
        /// measurement procedure:
        /// 1. Load the data to the machine.
        
        /// 2. Load sensor configuration.
        /// 3. Command the machine and sensors to begin the first step (RPM SWEEP)
        /// 4. Pass the rpm domain data to processing
        /// 5. Command the machine to run at the detected RPM[s]
        /// 6. pass the gathered time domain data to processing
        /// 7. Pass every bit of data to the database handler
        /// 8. send notification about the completion
        public void BeginExperiment()
        {
            throw new NotImplementedException();
        }

        public Task LoadExperiment(ExperimentSetup Setup)
        {

            _machineController.SetYPosition(Setup.MachineConfiguration.Y);

            return Task.CompletedTask;
        }
    }
}
