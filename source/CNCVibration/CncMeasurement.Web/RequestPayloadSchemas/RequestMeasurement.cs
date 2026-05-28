using CncMeasurement.Core.models;

namespace CncMeasurement.Web.RequestPayloadSchemas
{
    public class ExperimentRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public MachineConfig MachineConfig { get; set; }

        public List<AcquisitionConfig> Channels { get; set; }

        public ExperimentSetup ToExperiment()
        {
            ExperimentSetup Experiment = new ExperimentSetup();
            Experiment.ID = new Guid();
            Experiment.Name = Name;
            Experiment.Description = Description;
            Experiment.MachineConfig = MachineConfig;
            Experiment.MeasurementConfig = Channels;

            return Experiment;
        }
    }
}
