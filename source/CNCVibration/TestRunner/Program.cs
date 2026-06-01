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
            IDataAcquisitionService signalSource = new ImpulseSignalGenerator();
            var singleShotTrigger = new SingleShotTriggerService();
            var triggerDetector = new LevelTriggerDetector(22.0);

            var config = new AcquisitionConfig
            {
                SampleRate = 20000,
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
            var triggerConfig = new TriggerConfig
            {
                SampleRate = config.SampleRate,
                ChannelConfigs = config.ChannelConfigs,
                PreTriggerWindowMs = 1,
                PostTriggerWindowMs = 100
            };



            using var cts = new CancellationTokenSource();

            try
            {
                await signalSource.Start(config, cts.Token);

                // start trigger pipeline
                var triggerTask = singleShotTrigger.Start(
                    signalSource.Reader,
                    triggerConfig,
                    triggerDetector);

                // CSV writer task (consumes SignalWindow stream)
                var csvTask = Task.Run(async () =>
                {
                    int counter = 0;

                    await foreach (var window in singleShotTrigger.Reader.ReadAllAsync(cts.Token))
                    {
                        var analyzer = new ModalAnalyzer();
                        var result = analyzer.Analyze(window);

                        string rawPath = $"raw_{counter}.csv";
                        string fftPath = $"fft_{counter}.csv";

                        // 1. save raw signal
                        await WriteRawCsv(rawPath, window);

                        // 2. save FFT
                        await WriteFftCsv(fftPath, result.fft);

                        counter++;
                    }
                }, cts.Token);

                await triggerTask;
                await csvTask;
            }
            finally
            {
                cts.Cancel();
                await signalSource.StopAsync();
            }
        }

        static async Task WriteFftCsv(string path, FftFrame fft)
        {
            await using var writer = new StreamWriter(path);

            await writer.WriteLineAsync("frequency,channel,magnitude");

            foreach (var ch in fft.Channels)
            {
                for (int i = 0; i < fft.Frequencies.Length; i++)
                {
                    await writer.WriteLineAsync(
                        $"{fft.Frequencies[i]},{ch.AssignedChannelName},{ch.Bins[i].Magnitude}");
                }
            }
        }
        static async Task WriteRawCsv(string path, SignalFrame window)
        {
            await using var writer = new StreamWriter(path);

            await writer.WriteLineAsync("time,channel,value");

            double dt = 1.0 / window.SampleRate;
            int samples = window.Channels[0].Samples.Length;

            for (int i = 0; i < samples; i++)
            {
                double t = i * dt;

                for (int ch = 0; ch < window.Channels.Length; ch++)
                {
                    await writer.WriteLineAsync(
                        $"{t},{window.Channels[ch].AssignedChannelName},{window.Channels[ch].Samples[i]}");
                }
            }
        }

        static async Task TestAcquisition(IDataAcquisitionService DAQService)
        {
            var ProcessingService = new LiveSignalProcessor();

            await using var rmsCsv = new RmsCsvWriter("rms.csv");
            await using var fftCsv = new FftCsvWriter("fft.csv");

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
                await ProcessingService.Start(DAQService.Reader, cts.Token);

                var rmsTask = Task.Run(async () =>
                {
                    await foreach (var frame in ProcessingService.RMSReader.ReadAllAsync())
                    {
                        await rmsCsv.WriteAsync(frame);

                        Console.WriteLine($"RMS @ {frame.SampleIndex}");
                    }
                });
                var fftTask = Task.Run(async () =>
                {
                    await foreach (var frame in ProcessingService.FFTReader.ReadAllAsync())
                    {
                        await fftCsv.WriteAsync(frame);

                        Console.WriteLine($"FFT @ {frame.SampleIndex}");
                    }
                });

                Console.WriteLine($"Acquisition Started");
                await Task.Delay(10000);
                Console.WriteLine($"Acquisition Stopping");
                var stopProcessing = ProcessingService.StopAsync();
                var stopAcquisition = DAQService.StopAsync();

                await Task.WhenAll(stopAcquisition, stopProcessing, rmsTask, fftTask);

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

    public sealed class RmsCsvWriter : IAsyncDisposable
    {
        private readonly StreamWriter _writer;

        public RmsCsvWriter(string path)
        {
            _writer = new StreamWriter(path, append: false);
            _writer.WriteLine("Timestamp,SampleIndex,Channel,Value");
        }

        public async Task WriteAsync(RmsFrame frame)
        {
            foreach (var ch in frame.Channels)
            {
                await _writer.WriteLineAsync(
                    $"{frame.Timestamp:o},{frame.SampleIndex},{ch.AssignedChannelName},{ch.Value}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _writer.FlushAsync();
            _writer.Dispose();
        }
    }

    public sealed class FftCsvWriter : IAsyncDisposable
    {
        private readonly StreamWriter _writer;

        public FftCsvWriter(string path)
        {
            _writer = new StreamWriter(path, append: false);
            _writer.WriteLine("Timestamp,SampleIndex,Channel,Frequency,Magnitude");
        }

        public async Task WriteAsync(FftFrame frame)
        {
            int half = frame.Frequencies.Length;

            foreach (var ch in frame.Channels)
            {
                for (int i = 0; i < half; i++)
                {
                    await _writer.WriteLineAsync(
                        $"{frame.TimeStamp:o},{frame.SampleIndex},{ch.AssignedChannelName},{frame.Frequencies[i]},{ch.Bins[i].Magnitude}");
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _writer.FlushAsync();
            _writer.Dispose();
        }
    }
}
