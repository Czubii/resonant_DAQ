using CncMeasurement.Core.Interfaces;
using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CncMeasurement.Processing
{

    /// <summary>
    /// Allows to start the data processing after trigger has been hit. In the buffer stores specified amount of data chunks before trigger, and then
    /// sends forward those chunks + another specified amount of chunks after the trigger evenet. Automatically closes the channel after the data has been collected
    /// </summary>
    public class SingleShotTriggerService : Core.Interfaces.ISingleTriggerAcquisitionService
    {
        private Channel<SignalFrame> _outputChannel = Channel.CreateUnbounded<SignalFrame>();
        public ChannelReader<SignalFrame> Reader => _outputChannel.Reader;

        private Task _processingTask;
        private CancellationTokenSource _cts;

        public Task Start(ChannelReader<SampleChunk> input, TriggerConfig config, ITriggerDetector trigger, CancellationToken ct = default)
        {
            if (_processingTask != null)
                throw new Exception("Trigger acquisition already running");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            _processingTask = Task.Run(
                () => ProcessingLoop(input, config, trigger, _cts.Token));

            return Task.CompletedTask;
        }

        private async Task ProcessingLoop(ChannelReader<SampleChunk> input, TriggerConfig config,
     ITriggerDetector trigger, CancellationToken ct)
        {
            int preTriggerSamples = (int)(config.PreTriggerWindowMs * config.SampleRate / 1000);
            int postTriggerSamples = (int)(config.PostTriggerWindowMs * config.SampleRate / 1000);
            int totalSamples = preTriggerSamples + postTriggerSamples;

            var preBuffer = new CircularBuffer<double[]>(preTriggerSamples);

            bool triggered = false;
            double[][] measurement = null!;
            int writeIndex = 0;
            int numChannels = 0;

            DateTime outputWindowStartTime = DateTime.UtcNow;
            long outputWindowStartIndex = 0;

            double[] samplesPerChannel = null!;

            try
            {
                await foreach (var chunk in input.ReadAllAsync(ct))
                {
                    if (numChannels == 0)
                    {
                        numChannels = chunk.NumChannels;
                        samplesPerChannel = new double[numChannels]; // Delayed initialization
                    }

                    for (int i = 0; i < chunk.NumSamples; i++)
                    {
                        ct.ThrowIfCancellationRequested();

                        for (int ch = 0; ch < numChannels; ch++)
                        {
                            samplesPerChannel[ch] = chunk.Samples[ch, i];
                        }

                        // --- CASE 1: Searching for Trigger ---
                        if (!triggered)
                        {
                            preBuffer.Add((double[])samplesPerChannel.Clone());

                            if (trigger.IsTriggered(samplesPerChannel))
                            {
                                triggered = true;

                                int triggerGlobalIndex = (int)(chunk.SampleIndex + i);
                                outputWindowStartIndex = triggerGlobalIndex - preTriggerSamples;

                                var triggerTime = chunk.TimeStamp.AddSeconds((double)i / chunk.SampleRate);
                                outputWindowStartTime = triggerTime.AddSeconds(-preTriggerSamples / chunk.SampleRate);

                                // Prepare output structure
                                measurement = new double[numChannels][];
                                for (int ch = 0; ch < numChannels; ch++)
                                {
                                    measurement[ch] = new double[totalSamples];
                                }

                                var preSamples = preBuffer.ToArray();

                                for (int sampleIdx = 0; sampleIdx < preSamples.Length; sampleIdx++)
                                {
                                    var sample = preSamples[sampleIdx];
                                    for (int ch = 0; ch < numChannels; ch++)
                                    {
                                        measurement[ch][writeIndex] = sample[ch];
                                    }
                                    writeIndex++;
                                }
                            }
                            continue;
                        }

                        // --- CASE 2: Capturing Post-Trigger Data ---
                        for (int ch = 0; ch < numChannels; ch++)
                        {
                            measurement[ch][writeIndex] = samplesPerChannel[ch];
                        }
                        writeIndex++;

                        // Frame complete check
                        if (writeIndex >= totalSamples)
                        {
                            var channels = new SignalChannel[numChannels];
                            for (int ch = 0; ch < numChannels; ch++)
                            {
                                channels[ch] = new SignalChannel(
                                    config.ChannelConfigs[ch].NameToAssignToChannel,
                                    measurement[ch]
                                );
                            }

                            await _outputChannel.Writer.WriteAsync(new SignalFrame(
                                outputWindowStartIndex,
                                config.SampleRate,
                                outputWindowStartTime,
                                channels
                            ), ct);

                            // Exits the loop and terminates the method after 1 full capture window.
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _outputChannel.Writer.TryComplete(ex);
                throw; 
            }
            finally
            {
                _outputChannel.Writer.TryComplete();
            }
        }

        public async Task StopAsync()
        {
            if (_processingTask == null) return;

            _cts.Cancel();

            try
            {
                await _processingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }

            _outputChannel.Writer.TryComplete();

            _processingTask = null;
            _cts.Dispose();
            _cts = null;
        }
    }

    public sealed class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int _head;
        private int _count;

        public int Capacity => _buffer.Length;
        public int Count => _count;

        public CircularBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _buffer = new T[capacity];
        }

        public void Add(T item)
        {
            _buffer[_head] = item;

            _head = (_head + 1) % _buffer.Length;

            if (_count < _buffer.Length)
                _count++;
        }
        public T[] ToArray()
        {
            var result = new T[_count];

            for (int i = 0; i < _count; i++)
            {
                int index = (_head - _count + i + _buffer.Length) % _buffer.Length;
                result[i] = _buffer[index];
            }

            return result;
        }
    }
}
