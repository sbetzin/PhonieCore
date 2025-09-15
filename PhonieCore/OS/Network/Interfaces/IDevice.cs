using System;
using System.Threading.Tasks;
using Tmds.DBus;

namespace PhonieCore.OS
{
    public partial class NetworkManagerAdapter
    {
        [DBusInterface("org.freedesktop.NetworkManager.Device")]
        public interface IDevice : IDBusObject
        {
            Task<T> GetAsync<T>(string prop);
            Task<DeviceProperties> GetAllAsync();
            Task SetAsync(string prop, object val);
            Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
        }
    }
}
