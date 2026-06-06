namespace CncMeasurement.Core.Interfaces
{
    public interface IMachineController
    {
        void RunContinous(int RPM, float DurationSeconds);
        Task RunSweep();
        Task SetYPosition(double yPosition);
    }
}