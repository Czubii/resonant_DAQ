using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.Processing
{
    public interface IProcessing
    {
        public void ProcessSweep(string DataFilePath);
        public void Process(string DataFilePath);
    }
    public class Processor : IProcessing
    {
        public void Process(string DataFilePath)
        {
            throw new NotImplementedException();
        }

        public void ProcessSweep(string DataFilePath)
        {
            throw new NotImplementedException();
        }
    }
}
