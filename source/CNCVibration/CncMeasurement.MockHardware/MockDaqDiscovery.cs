using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.MockHardware
{
    public class MockDaqDiscovery : Core.Interfaces.IDaqDiscovery
    {
        public List<DeviceDescription> GetAvailableDevices()
        {
            var devices = new List<DeviceDescription>();

            string[] deviceNames = ["Dev0", "Dev0Mod0"];

            foreach (string name in deviceNames)
            {

                var desc = new DeviceDescription
                {
                    DeviceName = name,
                    AIChannels = [$"{name}/ai0", $"{name}/ai1", $"{name}/ai2", $"{name}/ai3"],
                    ProductType = "Mock Device",
                    SerialNumber = "Simulated/None"
                };

                devices.Add(desc);
            }

            return devices;

        }
    }
}
