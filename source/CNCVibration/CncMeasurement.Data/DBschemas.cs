using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Data
{
    internal class ExperimentSchema : ExperimentSetup
    {
        public List<PostProcessingResult> ProcessingResults { get; set; }
        public List<DateTime> PeakStamps { get; set; }
        public List<string> FFTpaths { get; set; }
        public List<string> RMSpaths { get; set; }
    }
}
