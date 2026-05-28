using CncMeasurement.Core.models;

namespace CncMeasurement.Web.RequestPayloadSchemas
{
    public class ExperimentRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public MachineConfig MachineConfig { get; set; }

        public AcquisitionConfig MeasurementConfig { get; set; }

        public ExperimentSetup ToExperiment()
        {
            ExperimentSetup Experiment = new ExperimentSetup();
            Experiment.ID = new Guid();
            Experiment.Name = Name;
            Experiment.Description = Description;
            Experiment.MachineConfiguration = MachineConfig;
            Experiment.MeasurementConfig = MeasurementConfig;

            return Experiment;
        }
    }
}
