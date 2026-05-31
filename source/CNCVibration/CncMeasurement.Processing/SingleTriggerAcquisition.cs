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
        private Channel<SampleChunk> _outputChannel = Channel.CreateUnbounded<SampleChunk>();
        public ChannelReader<SampleChunk> Reader => _outputChannel.Reader;

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

        private async Task ProcessingLoop(ChannelReader<SampleChunk> input, TriggerAcquisitionConfig config, ITriggerDetector trigger, CancellationToken ct)
        {
            var preBuffer = new CircularBuffer<SampleChunk>(config.PreTriggerChunks); //stores samples prior to trigger event

            bool triggered = false;
            int postCount = 0;

            try
            {
                await foreach (var chunk in input.ReadAllAsync(ct))
                {
                    preBuffer.Add(chunk);

                    if (!triggered && trigger.IsTriggered(chunk))// Write the elements from the buffer to output channel first
                    {
                        triggered = true;
                        postCount = 0;

                        foreach (var pre in preBuffer.ToArray()) 
                            await _outputChannel.Writer.WriteAsync(pre, ct);
                    }

                    if (triggered) // Write the chunks collected after being triggered
                    {
                        await _outputChannel.Writer.WriteAsync(chunk, ct);
                        postCount++;

                        if (postCount >= config.PostTriggerChunks)
                        {
                            break; // Stop 
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
