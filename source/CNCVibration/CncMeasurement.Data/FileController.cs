using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection.Metadata;
using System.Threading.Channels;

namespace CncMeasurement.Data
{
    sealed class FileWritingController
    {
        private string RMSPath;
        private StreamWriter _rmswriter;
        private string FFTPath;
        private StreamWriter _fftwriter;

        private string path = @"*\Experiments";
        public FileWritingController(ExperimentSetup ex)
        {
            
            path = Path.Combine(path, ex.ID.ToString());
            Directory.CreateDirectory(path);

            RMSPath = path + @"\rms.csv";
            FFTPath = path + @"\fft.csv";
            
        }
        public async Task WriteCompleteRMSAsync(ChannelReader<RmsFrame> RMSreader)
        {
            _rmswriter = new StreamWriter(RMSPath, true);
            _rmswriter.WriteLine("Timestamp,SampleIndex,Channel,Value");
            await foreach (var frame in RMSreader.ReadAllAsync())
            {
                foreach (var ch in frame.Channels)
                {
                    await _rmswriter.WriteLineAsync(
                        $"{frame.Timestamp:o},{frame.SampleIndex},{ch.AssignedChannelName},{ch.Value}");
                }
            }
        }
        public async Task WriteCompleteFFTAsync(ChannelReader<FftFrame> FFTreader)
        {
            _fftwriter = new StreamWriter(FFTPath, true);
            _fftwriter.WriteLine("Timestamp,SampleIndex,Channel,Frequency,Magnitude");

            await foreach (var frame in FFTreader.ReadAllAsync())
            {
                int half = frame.Frequencies.Length;
                foreach (var ch in frame.Channels)
                {
                    for (int i = 0; i < half; i++)
                    {
                        await _fftwriter.WriteLineAsync(
                            $"{frame.TimeStamp:o},{frame.SampleIndex},{ch.AssignedChannelName},{frame.Frequencies[i]},{ch.Bins[i].Magnitude}");
                    }
                }
            }
            
        }
        public async Task StopSaving()
        {
            await _rmswriter.FlushAsync();
            await _fftwriter.FlushAsync();
            _rmswriter.Dispose();
            _rmswriter.Dispose();
        }
    }
}
