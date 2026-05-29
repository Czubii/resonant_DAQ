using CncMeasurement.Core;
using CncMeasurement.Core.Interfaces;
using CncMeasurement.Core.models;
using CncMeasurement.Hardware;
using CncMeasurement.Hardware.Acquisition;
using CncMeasurement.Processing;
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
            var service = new CncMeasurement.MockHardware.MockDataAcquisitionService();
            await TestAcquisition(service);
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

        static async Task TestAcquisition(IDataAcquisitionService DAQService)
        {
            var ProcessingService = new LiveSignalProcessor();

            var config = new AcquisitionConfig
            {
                SampleRate = 10000,
                ChunkSize = 4096,
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
            
            try
            {
                await DAQService.Start(config, cts.Token);
                Console.WriteLine($"LOL");
                await ProcessingService.Start(DAQService.Reader, cts.Token);
                Console.WriteLine($"LOL");

                var printerClosed = PrintRMSAsync(ProcessingService.RMSReader, cts.Token);
                Console.WriteLine($"Acquisition Started");
                await Task.Delay(10000);
                Console.WriteLine($"Acquisition Stopping");
                var stopProcessing = ProcessingService.StopAsync();
                var stopAcquisition = DAQService.StopAsync();

                await Task.WhenAll(stopAcquisition, stopProcessing, printerClosed);

            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Acquisition stopped.");
            }
            finally
            {

                Console.WriteLine("Acquisition stopped.");
            }
        }
        static async Task PrintRMSAsync(ChannelReader<RmsFrame> reader, CancellationToken ct)
        {
            await foreach (var value in reader.ReadAllAsync(ct))
            {
                Console.WriteLine($"RMS Acceleration (Starting Sample: {value.SampleIndex}, Time Stamp: {value.Timestamp.TimeOfDay}):");
                foreach (var channel in value.Channels)
                {
                    Console.WriteLine($"{channel.AssignedChannelName}: {channel.Value}");
                }
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
