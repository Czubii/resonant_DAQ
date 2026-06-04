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
            await ExemplaryImpulseResponsePipeline();
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

        static async Task ExemplaryImpulseResponsePipeline()
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
                DampingSkipNAfterPeak = 1,
                UseNDominantModes = 8
            };

            var cts = new CancellationTokenSource();

            IDataAcquisitionService daq = new ModalAcquisitionService();
            IModalAnalyzer analyzer = new ModalAnalyzer();
            ITriggerWindowCapture trigger = new SingleTriggerWindowCapture();
            IModalExcelReportBuilder reportBuilder = new ModalExcelReportBuilder();

            var modalService = new ModalAnalysisService(daq, analyzer, trigger);

            var report = await modalService.RunAsync(config, triggerConfig, analysisConfig, cts.Token);

            await reportBuilder.BuildAsync(report,$"modal_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx", cts.Token);
        }

    }
}
