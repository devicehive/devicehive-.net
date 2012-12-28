using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeviceHive.Client;

namespace VirtualLedClient
{
    class Program
    {
        private const string LED_CODE = "LED";   // LED equipment code

        static void Main(string[] args)
        {
            try
            {
                // initialize logger
                log4net.Config.XmlConfigurator.Configure();

                // create a ClientService used to communicate with the DeviceHive service
                // insert your assigned DeviceHive service URL, username and password here
                var service = new RestfulClientService("http://localhost/DeviceHive.API", "dhadmin", "dhadmin_#911");

                // get information about VirtualLed device
                var deviceGuid = new Guid("E50D6085-2ABA-48E9-B1C3-73C673E414BE");
                var device = service.GetDevice(deviceGuid);
                if (device == null)
                {
                    Console.WriteLine("VirtualLed device does not exist on the server, please run VirtualLed device first!");
                    Console.ReadKey();
                    return;
                }
                Console.WriteLine("Found VirtualLed device with status: " + device.Status);

                // get information about current LED state
                var equipmentState = service.GetEquipmentState(device.Id.Value);
                var ledEquipmentState = equipmentState.FirstOrDefault(e => e.Id == LED_CODE);
                if (ledEquipmentState != null)
                {
                    Console.WriteLine("Current state of the VirtualLed: " + ledEquipmentState.GetParameter<int>("state"));
                }

                // subscribe to device notifications
                service.NotificationInserted += (s, e) => HandleNotification(e.Notification);
                service.SubscribeToNotifications(deviceGuid);
                
                service.ConnectionClosed += (s, e) =>
                {
                    if (service == null)
                        return;

                    while (true)
                    {
                        try
                        {
                            service.SubscribeToNotifications(deviceGuid);
                            break;
                        }
                        catch (ClientServiceException)
                        {
                            Thread.Sleep(100);
                        }
                    }                    
                };

                // read user input to send corresponding commands to the VirtualLed device
                Console.WriteLine("\nPlease enter a desired state of the led (either 0 or 1) or ESC to exit\n");
                while (true)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                        break;

                    if (key.KeyChar == '0' || key.KeyChar == '1')
                    {
                        Console.WriteLine(string.Format("Sending UpdateLedState command with state: {0}", key.KeyChar));
                        var command = new Command("UpdateLedState");
                        command.Parameter("equipment", LED_CODE);
                        command.Parameter("state", key.KeyChar);
                        service.SendCommand(device.Id.Value, command);
                    }
                }

                // unsubscribe from notifications
                service.Dispose();
                service = null;
            }
            catch (Exception ex)
            {
                // handle the error
                Console.WriteLine("Error: " + ex);
                Console.ReadKey();
            }
        }

        private static void HandleNotification(Notification notification)
        {
            if (notification.Name == "equipment" &&
                notification.GetParameter<string>("equipment") == LED_CODE)
            {
                var message = "Device sent LED state change notification, new state: {0}";
                Console.WriteLine(string.Format(message, notification.GetParameter<int>("state")));
            }
        }
    }
}
