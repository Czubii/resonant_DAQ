using CncMeasurement.Core;
using CncMeasurement.Core.models;
using CncMeasurement.Hardware;
using CncMeasurement.Hardware.Acquisition;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TestRunner
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            TestDiscovery();

            await TestAcquisition();
        }
        static void TestDiscovery()
        {
            var testDiscovery = new DaqDiscovery();
            Console.WriteLine("Avaialbe Devices:");
            var devices = testDiscovery.GetAvailableDevices();

            foreach (var device in devices)
            {
                Console.WriteLine(device.DeviceName);
                foreach (var ch in device.AIChannels)
                {
                    Console.WriteLine($"{ch}");
                }
            }
        }

        static async Task TestAcquisition()
        {
            var service = new NIDataAcquisitionService();

            var config = new AcquisitionConfig
            {
                SampleRate = 10000,
                ChunkSize = 1000,
                GroupName = "test",
                OutputTDMSPath = "tetstoutput.tdms",
                ChannelConfigs = new List<ChannelConfig>
            {
                new ChannelConfig
                {
                    PhysicalChannelName = "cDAQ1Mod1/ai0",
                    NameToAssignToChannel = "Accel X",
                    MinRange = -50,
                    MaxRange = 50,
                    Sensitivity = 100,
                 
                },
                new ChannelConfig
                {
                    PhysicalChannelName = "cDAQ1Mod1/ai1",
                    NameToAssignToChannel = "Accel Y",
                    MinRange = -50,
                    MaxRange = 50,
                    Sensitivity = 100,
                }
            }
            };

            using var cts = new CancellationTokenSource();

            // stop after 5 seconds (for testing)
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                await service.StartAsync(config);
                var readerTask = PrintChunksAsync(service.Reader, cts.Token);
                await Task.Delay(1000);

                await service.StopAsync();

                await readerTask;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Acquisition stopped.");
            }
        }
        static async Task PrintChunksAsync(ChannelReader<SampleChunk> reader, CancellationToken ct)
        {
            await foreach (var chunk in reader.ReadAllAsync(ct))
            {
                PrintChunk(chunk);
            }
        }
        static void PrintChunk(SampleChunk chunk)
        {
            Console.WriteLine($"Chunk @ {chunk.SampleIndex} | samples: {chunk.NumSamples} | channels: {chunk.NumChannels}");

            for (int ch = 0; ch < chunk.NumChannels; ch++)
            {
                Console.Write($"CH{ch}: ");

                for (int i = 0; i < Math.Min(5, chunk.NumSamples); i++)
                {
                    Console.Write($"{chunk.Samples[ch, i]:F3} ");
                }

                Console.WriteLine();
            }

            Console.WriteLine("-----------------------------------");
        }
    }
}
