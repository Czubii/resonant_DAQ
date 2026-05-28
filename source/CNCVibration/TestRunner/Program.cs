using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CncMeasurement.Hardware;
using CncMeasurement.Core;
using System.Diagnostics.Contracts;

namespace TestRunner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TestDiscovery();
        }
        static void TestDiscovery()
        {
            var testDiscovery = new DaqDiscovery();
            Console.WriteLine("Avaialbe Devices:");
            var devices = testDiscovery.GetAvailableDevices();

            foreach (var device in devices)
            {
                Console.WriteLine(device.DeviceName);
                foreach (var ch in device.AIChannels)
                {
                    Console.WriteLine($"{ch}");
                }
            }
        }
    }
}
