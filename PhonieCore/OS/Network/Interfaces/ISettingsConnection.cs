using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace PhonieCore.OS
{
    public partial class NetworkManagerAdapter
    {
        [DBusInterface("org.freedesktop.NetworkManager.Settings.Connection")]
        public interface ISettingsConnection : IDBusObject
        {
            Task<IDictionary<string, IDictionary<string, object>>> GetSettingsAsync();
            Task UpdateAsync(IDictionary<string, IDictionary<string, object>> settings);
        }
    }
}
