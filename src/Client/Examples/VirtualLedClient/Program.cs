using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeviceHive.Client;
using Newtonsoft.Json.Linq;

namespace VirtualLedClient
{
    class Program
    {
        private const string LED_CODE = "LED";   // LED equipment code

        static void Main(string[] args)
        {
            try
            {
                VirtualLedClientRoutine().Wait();
            }
            catch (Exception ex)
            {
                // handle the error
                Console.WriteLine("Error: " + ex);
                Console.ReadKey();
            }
        }

        static async Task VirtualLedClientRoutine()
        {
            // create a DeviceHiveConnectionInfo object
            // insert your assigned DeviceHive service URL, username and password here
            var connectionInfo = new DeviceHiveConnectionInfo("http://localhost/DeviceHive.API", "dhadmin", "dhadmin_#911");

            // create a DeviceHiveClient object used to communicate with the DeviceHive service
            var client = new DeviceHiveClient(connectionInfo);

            // get information about the VirtualLed device
            var deviceGuid = "E50D6085-2ABA-48E9-B1C3-73C673E414BE";
            var device = await client.GetDeviceAsync(deviceGuid);
            if (device == null)
            {
                Console.WriteLine("VirtualLed device does not exist on the server, please run VirtualLed device first!");
                Console.ReadKey();
                return;
            }
            Console.WriteLine("Found VirtualLed device with status: " + device.Status);

            // get information about current LED state
            var equipmentState = await client.GetEquipmentStateAsync(device.Id);
            var ledEquipmentState = equipmentState.FirstOrDefault(e => e.Id == LED_CODE);
            if (ledEquipmentState != null)
            {
                Console.WriteLine("Current state of the VirtualLed: " + ledEquipmentState.GetParameter<int>("state"));
            }

            // subscribe to device notifications
            var subscription = await client.AddNotificationSubscriptionAsync(new[] { deviceGuid }, null, HandleNotification);

            // read user input to send corresponding commands to the VirtualLed device
            Console.WriteLine("\nPlease enter a desired state of the led (either 0 or 1) or ESC to exit\n");
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                    break;

                if (key.KeyChar == '0' || key.KeyChar == '1')
                {
                    // send a command to the VirtualLed device to switch the LED state
                    Console.WriteLine(string.Format("Sending UpdateLedState command with state: {0}", key.KeyChar));
                    var command = new Command("UpdateLedState");
                    command.Parameter("equipment", LED_CODE);
                    command.Parameter("state", key.KeyChar);
                    await client.SendCommandAsync(device.Id, command);
                }
            }

            // unsubscribe from notifications and dispose the client
            await client.RemoveSubscriptionAsync(subscription);
            client.Dispose();
        }

        private static void HandleNotification(DeviceNotification deviceNotification)
        {
            // get the notification object
            var notification = deviceNotification.Notification;

            if (notification.Name == "equipment" && notification.GetParameter<string>("equipment") == LED_CODE)
            {
                // output the current LED state
                var message = "Device sent LED state change notification, new state: {0}";
                Console.WriteLine(string.Format(message, notification.GetParameter<int>("state")));
            }
            else if (notification.Name == "$device-update" && notification.GetParameter<string>("status") != null)
            {
                var message = "Device changed the status, new status: {0}";
                Console.WriteLine(string.Format(message, notification.GetParameter<string>("status")));
            }
        }
    }
}
