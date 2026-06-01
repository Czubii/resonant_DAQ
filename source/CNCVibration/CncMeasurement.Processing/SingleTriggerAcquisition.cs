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
    public class SingleTriggerAcquisitionService : Core.Interfaces.ISingleTriggerAcquisitionService
    {
        private Channel<SignalWindow> _outputChannel = Channel.CreateUnbounded<SignalWindow>();
        public ChannelReader<SignalWindow> Reader => _outputChannel.Reader;

        private Task _processingTask;
        private CancellationTokenSource _cts;

        public Task Start(ChannelReader<SampleChunk> input, TriggerAcquisitionConfig config, ITriggerDetector trigger, CancellationToken ct = default)
        {
            if (_processingTask != null)
                throw new Exception("Trigger acquisition already running");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            _processingTask = Task.Run(
                () => ProcessingLoop(input, config, trigger, _cts.Token));

            return Task.CompletedTask;
        }

        private async Task ProcessingLoop(ChannelReader<SampleChunk> input, TriggerAcquisitionConfig config,
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
            try
            {
                await foreach (var chunk in input.ReadAllAsync(ct))
                {
                    numChannels = chunk.NumChannels;

                    for (int i = 0; i < chunk.NumSamples; i++)
                    {
                        ct.ThrowIfCancellationRequested();

                        var samplesPerChannel = new double[numChannels];

                        for (int ch = 0; ch < numChannels; ch++)
                        {
                            samplesPerChannel[ch] = chunk.Samples[ch, i];
                        }
                        
                        // Capture pre trigger samples + trigger detection:
                        if (!triggered)
                        {
                            preBuffer.Add(samplesPerChannel);

                            if (trigger.IsTriggered(samplesPerChannel)) // The indentation here is kinda impressive
                            {
                                triggered = true;

                                // Compute the metadata for the output:

                                int triggerGlobalIndex = (int)(chunk.SampleIndex + i);
                                outputWindowStartIndex = triggerGlobalIndex - preTriggerSamples;

                                var triggerTime = chunk.TimeStamp.AddSeconds((double)i / chunk.SampleRate);

                                outputWindowStartTime = triggerTime.AddSeconds(-preTriggerSamples / chunk.SampleRate);


                                // prepare the output array:
                                measurement = new double[numChannels][];

                                for (int ch = 0; ch < numChannels; ch++)
                                {
                                    measurement[ch] = new double[totalSamples];
                                }

                                var preSamples = preBuffer.ToArray();

                                int start = Math.Max(0, preSamples.Length - preTriggerSamples);

                                // copy the buffer into output measurement array
                                for (int sampleIdx = start; sampleIdx < preSamples.Length; sampleIdx++)
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

                        // Capture post-trigger samples

                        for (int ch = 0; ch < numChannels; ch++)
                        {
                            measurement[ch][writeIndex] = samplesPerChannel[ch];
                        }

                        writeIndex++;

                        if (writeIndex >= totalSamples)
                        {
                            await _outputChannel.Writer.WriteAsync(
                                new SignalWindow
                                (
                                    outputWindowStartIndex,
                                    measurement.Length,
                                    config.ChannelConfigs.Select(a => a.NameToAssignToChannel).ToArray(),
                                    config.SampleRate,
                                    outputWindowStartTime,
                                    measurement
                                ),ct);

                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _outputChannel.Writer.TryComplete(ex);
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
