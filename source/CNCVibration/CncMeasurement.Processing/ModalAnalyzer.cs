using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
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


            return new ModalResults { fft = fftSpectrum };
        }
    }
}
