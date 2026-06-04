using CncMeasurement.Core;
using CncMeasurement.Core.Interfaces;
using CncMeasurement.Core.models;
using CncMeasurement.Hardware;
using CncMeasurement.Hardware.Acquisition;
using CncMeasurement.MockHardware;
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
            await ConsoleUiLoop();
        }
        static async Task ConsoleUiLoop()
        {
            var cts = new CancellationTokenSource();

            while (true)
            {
                Console.WriteLine("\n=== CNC Measurement Console ===");
                Console.WriteLine("1 - Run single measurement");
                Console.WriteLine("2 - Test device discovery");
                Console.WriteLine("3 - Run continuous measurements");
                Console.WriteLine("4 - Stop continuous run");
                Console.WriteLine("q - Quit");
                Console.Write("Select: ");

                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        await RunSingleMeasurement(cts.Token);
                        break;

                    case "2":
                        TestDiscovery();
                        break;

                    case "3":
                        cts = new CancellationTokenSource();
                        _ = RunContinuousMeasurements(cts.Token);
                        break;

                    case "4":
                        cts.Cancel();
                        Console.WriteLine("Stopping continuous run...");
                        break;

                    case "q":
                        cts.Cancel();
                        return;

                    default:
                        Console.WriteLine("Unknown command.");
                        break;
                }
            }
        }
        static async Task RunSingleMeasurement(CancellationToken token)
        {
            Console.WriteLine("Running single measurement...");

            await ExemplaryImpulseResponsePipeline(token);

            Console.WriteLine("Measurement finished.");
        }
        static async Task RunContinuousMeasurements(CancellationToken token)
        {
            Console.WriteLine("Continuous mode started. Press '4' to stop.");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await ExemplaryImpulseResponsePipeline(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                Console.WriteLine("Cycle complete. Restarting...\n");
            }

            Console.WriteLine("Continuous mode stopped.");
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

        static async Task ExemplaryImpulseResponsePipeline(CancellationToken token)
        {
            var config = new AcquisitionConfig
            {
                SampleRate = 15000,
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
                    },
                    new ChannelConfig
                    {
                        PhysicalChannelName = "cDAQ1Mod1/ai1",
                        NameToAssignToChannel = "Accel Z",
                        MinRange = -50,
                        MaxRange = 50,
                        Sensitivity = 100,
                    }
                }
            };

            var triggerConfig = new TriggerConfig
            {
                SampleRate = config.SampleRate,
                ChannelConfigs = config.ChannelConfigs,
                PreTriggerWindowMs = 250,
                PostTriggerWindowMs = 250,
                Threshold = 1.0
            };

            var analysisConfig = new ModalAnalysisConfig
            {
                ModeProminenceThresholddB = 2,
                DampingFilterBandwidthPercent = 0.2,
                DampingStartPeakPercent = 0.95f,
                DampingEndPeakPercent = 0.05f,
                UseNDominantModes = 8
            };

            IDataAcquisitionService daq = new ModalAcquisitionService();
            IModalAnalyzer analyzer = new ModalAnalyzer();
            ITriggerWindowCapture trigger = new SingleTriggerWindowCapture();
            IModalExcelReportBuilder reportBuilder = new ModalExcelReportBuilder();

            var modalService = new ModalAnalysisService(daq, analyzer, trigger, reportBuilder);

            var results = await modalService.RunAsync(config, triggerConfig, analysisConfig, token);


        }

    }
}
