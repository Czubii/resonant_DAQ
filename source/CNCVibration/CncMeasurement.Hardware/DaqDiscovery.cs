using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.DAQmx;
using CncMeasurement.Core.models;

namespace CncMeasurement.Hardware
{
    public interface IDaqDiscovery
    {
        List<DeviceDescription> GetAvailableDevices();
    }
    public class DaqDiscovery : IDaqDiscovery
    {
        public List<DeviceDescription> GetAvailableDevices()
        {
            var devices = new List<DeviceDescription>();

            try
            {
                // get the information about devices from NI MAX using DaqSystem.Local
                string[] deviceNames = DaqSystem.Local.Devices;

                foreach (string name in deviceNames)
                {
                    Device currentDevice = DaqSystem.Local.LoadDevice(name);

                    var desc = new DeviceDescription
                    {
                        DeviceName = name,
                        AIChannels = currentDevice.AIPhysicalChannels.ToList<string>(),
                        ProductType = currentDevice.ProductType,
                        SerialNumber = currentDevice.SerialNumber != 0
                            ? currentDevice.SerialNumber.ToString("X")
                            : "Simulated/None"
                    };

                    devices.Add(desc);
                }
            }
            catch (DaqException ex)
            {
                Console.WriteLine($"Failed to read available NI DAQ devices: {ex.Message}");
                // Fallback for testing if NI-DAQmx drivers are missing.
                devices.Add(new DeviceDescription { DeviceName = "MockDevice", ProductType = "Simulated Card" });
            }
            return devices;
        }
    }
}
