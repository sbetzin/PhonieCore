using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace PhonieCore.OS
{
    public partial class NetworkManagerAdapter
    {
        [DBusInterface("org.freedesktop.NetworkManager.Settings")]
        public interface ISettings : IDBusObject
        {
            Task<ObjectPath[]> ListConnectionsAsync();
            Task<ObjectPath> AddConnectionAsync(IDictionary<string, IDictionary<string, object>> settings);
        }
    }
}
