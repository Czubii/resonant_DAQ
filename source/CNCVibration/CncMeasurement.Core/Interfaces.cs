using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using CncMeasurement.Core.models;

namespace CncMeasurement.Core.Interfaces
{

    public interface IMeasurementClient
    {
        Task ReceiveMeasurement(SampleChunk data);
        Task ReceiveSystemStatus(string status);
    }
    public interface IMeasurementBroadcaster
    {
        Task BroadcastMeasurementAsync(SampleChunk data, CancellationToken ct);
    }
    public interface IDataAcquisitionService
    {
        public Task StartAsync(AcquisitionConfig config, CancellationToken ct = default);
        public Task StopAsync();
        public ChannelReader<SampleChunk> Reader { get; }
    }
    public interface IEngine
    {
        void LoadExperiment(ExperimentSetup Setup);
        Task RunExperiment(ChannelReader<SampleChunk> Reader, CancellationToken ct);
        
    }
    public interface IDatabaseController
    {
        DBinfo listCollections();
        void InitializeCollections();
        void ClearDatabase();
        void AddMeasurementEntry(MeasurementMetadata MeasuredData);
        MeasurementMetadata GetMeasurementByID(int measurementID);

        List<BriefMeasurementInfo> GetMeasurementSummaries();
        ValueTask QueueForSavingAsync(SampleChunk data, CancellationToken ct);
        ValueTask QueueForSavingSweepAsync(SampleChunk data, CancellationToken ct);

    }
    public interface IMachineController
    {
        Task SetYPosition(double yPosition);
        Task RunSweep();
        void RunContinous(int RPM);

        Task Stop();

    }
    public interface IProcessing
    {
        ValueTask QueueForProcessingAsync(SampleChunk data, CancellationToken ct);
        public void ProcessSweep(string DataFilePath);
        public void Process(string DataFilePath);
    }
}
