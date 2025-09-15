using System;
using System.Threading.Tasks;
using Tmds.DBus;

namespace PhonieCore.OS
{
    public partial class NetworkManagerAdapter
    {
        // --------- Minimal nötige D-Bus Interfaces ----------

        [DBusInterface("org.freedesktop.NetworkManager")]
        public interface INetworkManager : IDBusObject
        {
            Task<ObjectPath[]> GetDevicesAsync();
            Task<IDisposable> WatchStateChangedAsync(Action<uint> handler);
            Task<T> GetAsync<T>(string prop);
            Task<NetworkManagerProperties> GetAllAsync();
            Task SetAsync(string prop, object val);
            Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
        }
    }
}
