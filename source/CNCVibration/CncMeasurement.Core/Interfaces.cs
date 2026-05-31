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
    public interface IEngine
    {
        void LoadExperiment(ExperimentSetup Setup);
        Task RunExperiment(ChannelReader<SampleChunk> Reader, CancellationToken ct);

    }
    public interface IDatabaseController
    {
        void InitializeContext();
        Task StartLogLiveExperiment(ExperimentSetup setup, ChannelReader<RmsFrame> RMSreader, ChannelReader<FftFrame> FFTreader);
        Task StopLog();
        DBinfo listCollections();
        
      
        void ClearDatabase();
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
    public interface IDataAcquisitionService: IAsyncDisposable
    {
        public Task Start(AcquisitionConfig config, CancellationToken ct = default);
        public Task StopAsync();
        public ChannelReader<SampleChunk> Reader { get; }
    }
    public interface ILiveSignalProcessor : IAsyncDisposable
    {
        public Task Start(ChannelReader<SampleChunk> sampleChunkReader, CancellationToken ct = default);
        public Task StopAsync();
        public ChannelReader<FftFrame> FFTReader { get; }
        public ChannelReader<RmsFrame> RMSReader { get; }
    }
    public interface IDaqDiscovery
    {
        List<DeviceDescription> GetAvailableDevices();
    }
    public interface ITriggerDetector
    {
        bool IsTriggered(SampleChunk chunk);
    }
    public interface ISingleTriggerAcquisitionService
    {
        public Task Start(ChannelReader<SampleChunk> input, TriggerAcquisitionConfig config, ITriggerDetector trigger, CancellationToken ct = default);
        public Task StopAsync();
        public ChannelReader<SampleChunk> Reader { get; }
    }
}
