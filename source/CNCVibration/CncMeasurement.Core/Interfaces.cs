using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using CncMeasurement.Core.models;

namespace CncMeasurement.Core.Interfaces
{
    public interface IDataAcquisitionService
    {
        public Task StartAsync(AcquisitionConfig config, CancellationToken ct = default);
        public Task StopAsync();
        public ChannelReader<SampleChunk> Reader { get; }
    }
}
