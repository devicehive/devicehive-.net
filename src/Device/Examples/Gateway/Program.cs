using System;
using System.IO.Ports;
using DeviceHive.Binary;
using DeviceHive.Device;

namespace Gateway
{
    internal class Program
    {
        private static void Main()
        {
            try
            {
                // initialize logger
                log4net.Config.XmlConfigurator.Configure();

                // create a RestfulDeviceService used to communicate with the DeviceHive cloud
                // insert your assigned DeviceHive service URL here
                using (var deviceService = new RestfulDeviceService("http://localhost/DeviceHive.API"))
                {
                    // create a DeviceHive network where our gateway will reside
                    var network = new Network("Gateway Sample Network", "A DeviceHive network for Gateway sample");

                    // create gateway service
                    var gatewayService = new GatewayService(deviceService, network);
                    
                    // create connection to device through COM port and add it to the gateway
                    // insert your COM port name here
                    var serialPort = new SerialPort("COM3") {ReadTimeout = 1000, WriteTimeout = 1000};
                    var serialPortConnection = new SerialPortBinaryConnection(serialPort);
                    gatewayService.DeviceConnectionList.Add(serialPortConnection);

                    // start gateway
                    gatewayService.Start();

                    // wait for console key press and then dispose gateway service
                    Console.WriteLine("Gateway is now running, press any key to stop...");
                    Console.ReadKey();
                    gatewayService.Stop();
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
