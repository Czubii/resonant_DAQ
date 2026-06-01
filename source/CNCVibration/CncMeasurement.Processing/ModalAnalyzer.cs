using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Processing
{
    public class ModalResults
    {
        public FftFrame fft {  get; set; }
    }
    public class ModalAnalyzer
    {
        public ModalResults Analyze(SignalWindow signalWindow)
        {
            var fftSpectrum = FFTSpectrum.Compute(signalWindow);

            var dampingRatios = DampingEstimator.Compute(signalWindow, 500, 10);

            for (int ch = 0; ch<signalWindow.NumChannels; ch++)
            {
                Console.WriteLine($"Channel {signalWindow.AssignedChannelNames[ch]} | Damping Ratio: {dampingRatios[ch]}");
            }

            return new ModalResults { fft = fftSpectrum };
        }
    }
}
