namespace CncMeasurement.Machine
{
    public interface IMachineController
    {
        Task SetYPosition(double yPosition);
        void RunSweep();
        void RunContinous(int RPM, float DurationSeconds);

    }
    public class MachineController : IMachineController
    {
        public void RunContinous(int RPM, float DurationSeconds)
        {
            throw new NotImplementedException();
        }

        public void RunSweep()
        {
            throw new NotImplementedException();
        }

        public Task SetYPosition(double yPosition)
        {
            throw new NotImplementedException();
        }
    }
}
