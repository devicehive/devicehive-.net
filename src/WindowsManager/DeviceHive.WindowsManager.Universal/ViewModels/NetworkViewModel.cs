using DeviceHive.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHive.WindowsManager.ViewModels
{
    class NetworkViewModel : Network
    {
        public NetworkViewModel(Network network)
        {
            Id = network.Id;
            Name = network.Name;
            Description = network.Description;
        }

        public List<Device> Devices { get; set; }
    }
}
