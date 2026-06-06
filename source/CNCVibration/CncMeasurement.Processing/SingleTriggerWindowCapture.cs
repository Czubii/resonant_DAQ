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
    public class SingleTriggerWindowCapture : ITriggerWindowCapture
    {
        public async Task<SignalFrame> SingleCapture(ChannelReader<SampleChunk> input, TriggerConfig config, CancellationToken ct)
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

                        if (IsTriggered(samplesPerChannel, config))
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

                        return new SignalFrame(
                            outputWindowStartIndex,
                            config.SampleRate,
                            outputWindowStartTime,
                            channels
                        );

                    }
                }
            }

            throw new Exception("Capture failed.");
        }

        public bool IsTriggered(double[] samples, TriggerConfig config)
        {
            int channels = samples.Length;

            for (int c = 0; c < channels; c++)
            {
                if (Math.Abs(samples[c]) >= config.Threshold)
                {
                    return true;
                }
            }
            return false;
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
