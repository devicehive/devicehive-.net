using System;
using System.Linq;
using System.Threading;
using DeviceHive.Device;

namespace VirtualLed
{
    /// <summary>
    /// Represents VirtualLed device implementation
    /// </summary>
    [Device("E50D6085-2ABA-48E9-B1C3-73C673E414BE", "05F94BF509C8", "Sample VirtualLed Device")]
    [DeviceClass("Sample VirtualLed Device", "1.0", OfflineTimeout = 600)]
    [DeviceEquipment(VirtualLedDevice.LED_CODE, "VirtualLED", "Controllable LED")]
    public class VirtualLedDevice : DeviceBase
    {
        private const string LED_CODE = "LED";   // LED equipment code
        private int _ledState = 0;               // LED state

        #region DeviceBase Members

        /// <summary>
        /// Represents main device thread.
        /// </summary>
        /// <param name="token">Thread cancellation token.</param>
        public override void Main(CancellationToken token)
        {
            // send the notification about initial LED state
            ServiceChannel.SendNotification(new LedNotification(LED_CODE, _ledState));
            
            // do nothing in the main thread
        }
        #endregion

        #region Command Actions

        /// <summary>
        /// Update LED state command handler
        /// </summary>
        /// <param name="command">UpdateLedStateCommand object with command parameters.</param>
        /// <param name="token">Thread cancellation token.</param>
        /// <returns>DeviceCommandResult with command execution result.</returns>
        [DeviceCommand("UpdateLedState")]
        public DeviceCommandResult UpdateLedState(UpdateLedStateCommand command, CancellationToken token)
        {
            // validate input parameters
            if (command.Equipment != LED_CODE)
                return new DeviceCommandResult("Failed", "Invalid equipment parameter: " + command.Equipment);
            if (command.State == null || !new[] { 0, 1 }.Contains(command.State.Value))
                return new DeviceCommandResult("Failed", "Invalid state parameter: " + command.State);

            // switch the LED state
            Console.WriteLine("Received UpdateLedState command, setting LED state to " + command.State);
            _ledState = command.State.Value;

            // send the notification about LED state update
            ServiceChannel.SendNotification(new LedNotification(command.Equipment, command.State.Value));

            // return the Completed status
            return new DeviceCommandResult("Completed");
        }
        #endregion
    }
}
    