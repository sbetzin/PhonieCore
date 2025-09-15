using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace PhonieCore.OS
{
    public partial class NetworkManagerAdapter
    {
        [DBusInterface("org.freedesktop.NetworkManager.Device.Wireless")]
        public interface IWireless : IDBusObject
        {
            Task RequestScanAsync(IDictionary<string, object> options);
        }
    }
}
