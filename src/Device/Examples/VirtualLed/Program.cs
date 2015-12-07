﻿using System;
using DeviceHive.Device;

namespace VirtualLed
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // initialize logger
                log4net.Config.XmlConfigurator.Configure();

                // create a RestfulDeviceService used to communicate with the DeviceHive cloud
                // insert your assigned DeviceHive service URL here
                using (var service = new RestfulDeviceService("http://localhost/DeviceHive.API"))
                {
                    // create a DeviceHive network where our device will reside
                    var network = new Network("VirtualLed Sample Network", "A DeviceHive network for VirtualLed sample");
                    // or use network with network key
                    //var network = new Network("Network WPNBEP", "Playground Network", "%NETWORK_KEY%");

                    // create a DeviceHost used to run our device
                    var deviceHost = new DeviceHost(service, network);

                    // create an instance of virtual LED device and add it to the host
                    var led = new VirtualLedDevice();
                    deviceHost.AddDevice(led);

                    // start the host - it will register the device and start polling commands
                    deviceHost.Start();

                    // wait for console key press and then stop the host
                    Console.WriteLine("Device is now running, press any key to stop...");
                    Console.ReadKey();
                    deviceHost.Stop();
                }
            }
            catch (Exception ex)
            {
                // handle the error
                Console.WriteLine("Error: " + ex);
                Console.ReadKey();
            }
        }
    }
}
