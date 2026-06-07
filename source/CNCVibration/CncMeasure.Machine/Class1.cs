using CncMeasurement.Core.Interfaces;

namespace CncMeasurement.Machine
{
    
    public class MachineController : IMachineController
    {
        public void RunContinous(int RPM)
        {
            throw new NotImplementedException();
        }

        public Task RunSweep()
        {
            throw new NotImplementedException();
        }

        public Task SetYPosition(double yPosition)
        {
            throw new NotImplementedException();
        }

        public Task Stop()
        {
            throw new NotImplementedException();
        }
    }
}
