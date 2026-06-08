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

    public class ModalAnalysisRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public MachineConfig MachineConfig { get; set; }
        public AcquisitionConfig MeasurementConfig { get; set; }
        public TriggerConfig TriggerConfig { get; set; }
        public ModalAnalysisConfig AnalysisConfig { get; set; }

        public ModalAnalysisExperimentSetup ToExperiment()
        {
            ModalAnalysisExperimentSetup Experiment = new ModalAnalysisExperimentSetup();
            
            Experiment.Name = Name;
            Experiment.Description = Description;
            Experiment.MachineConfig = MachineConfig;
            Experiment.MeasurementConfig = MeasurementConfig;
            Experiment.TriggerConfig = TriggerConfig;
            Experiment.AnalysisConfig = AnalysisConfig;
            return Experiment;
        }

    }
}
