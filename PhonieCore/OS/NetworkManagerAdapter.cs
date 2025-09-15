using PhonieCore.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace PhonieCore.OS
{
    public class NetworkManagerAdapter
    {
        private INetworkManager _networkManager;
        private ISettings _settings;
        private Connection _bus;
        private string _ifname;

        private bool disposedValue;

        public async Task StartAsync(string ifname)
        {
            try
            {
                Logger.Log($"Starting Network Manager to interface {ifname}"); 
                _ifname = ifname;
                _bus = new Connection(Address.System);
                await _bus.ConnectAsync();

                _networkManager = _bus.CreateProxy<INetworkManager>("org.freedesktop.NetworkManager", "/org/freedesktop/NetworkManager");
                _settings = _bus.CreateProxy<ISettings>("org.freedesktop.NetworkManager", "/org/freedesktop/NetworkManager/Settings");

                uint state = await _networkManager.GetAsync<uint>("State");
                Console.WriteLine($"Initial State: {state}");

                await _networkManager.WatchStateChangedAsync(NetworkStateChange);
            }
            catch (Exception e)
            {
                Logger.Error($"Could not connect to network manager: {e.Message}");
            }
        }

        private void NetworkStateChange(uint newState)
        {
            Logger.Log($"State changed: new={newState}");
        }

        public async Task TryConnectAsync()
        {
            try
            {
                Logger.Log($"Requesting rescan for interface {_ifname}");
                var devicePath = await FindDeviceByIfnameAsync(_ifname)
                ?? throw new InvalidOperationException($"Device {_ifname} nicht gefunden.");

                var wifi = _bus.CreateProxy<IWireless>("org.freedesktop.NetworkManager", devicePath);
                await wifi.RequestScanAsync(new Dictionary<string, object>());
            }
            catch (Exception e)
            {
                Logger.Error($"Could not request a rescan: {e.Message}");

            }
        }

        public async Task<ObjectPath> EnsureWifiProfileAsync(string name, string ssid, string psk, bool hidden = false)
        {
            var existing = await FindConnectionByIdAsync(name);
            var settings = BuildWifiSettings(name, ssid, psk, hidden);

            if (existing == null)
            {
                return await _settings.AddConnectionAsync(settings);
            }
            else
            {
                var settingsConnection = _bus.CreateProxy<ISettingsConnection>("org.freedesktop.NetworkManager", existing.Value);
                await settingsConnection.UpdateAsync(settings);
                return existing.Value;
            }
        }

        private async Task<ObjectPath?> FindDeviceByIfnameAsync(string ifname)
        {
            var devices = await _networkManager.GetDevicesAsync();
            foreach (var d in devices)
            {
                var dev = _bus.CreateProxy<IDevice>("org.freedesktop.NetworkManager", d);
                var name = await dev.GetAsync<string>("Interface");
                if (string.Equals(name, ifname, StringComparison.Ordinal))
                    return d;
            }
            return null;
        }

        private async Task<ObjectPath?> FindConnectionByIdAsync(string id)
        {
            var connectsions = await _settings.ListConnectionsAsync();
            foreach (var connection in connectsions)
            {
                var settingsConnection = _bus.CreateProxy<ISettingsConnection>("org.freedesktop.NetworkManager", connection);
                var settings = await settingsConnection.GetSettingsAsync();
                if (settings.TryGetValue("connection", out var conn) &&
                    conn.TryGetValue("id", out var val) && val is string cid &&
                    string.Equals(cid, id, StringComparison.Ordinal))
                {
                    return connection;
                }
            }
            return null;
        }

        private IDictionary<string, IDictionary<string, object>> BuildWifiSettings(string name, string ssid, string psk, bool hidden)
        {
            var ssidBytes = System.Text.Encoding.UTF8.GetBytes(ssid);

            var settings = new Dictionary<string, IDictionary<string, object>>
            {
                ["connection"] = new Dictionary<string, object>
                {
                    ["id"] = name,
                    ["type"] = "802-11-wireless",
                    ["autoconnect"] = true,
                    ["autoconnect-retries"] = 10,
                    ["interface-name"] = _ifname
                },
                ["802-11-wireless"] = new Dictionary<string, object>
                {
                    ["ssid"] = ssidBytes,
                    ["mode"] = "infrastructure",
                    ["hidden"] = hidden
                },
                ["802-11-wireless-security"] = new Dictionary<string, object>
                {
                    ["key-mgmt"] = "wpa-psk",
                    ["psk"] = psk
                },
                ["ipv4"] = new Dictionary<string, object>
                {
                    ["method"] = "auto"
                },
                ["ipv6"] = new Dictionary<string, object>
                {
                    ["method"] = "ignore"
                }
            };

            return settings;
        }

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

        [Dictionary]
        public class NetworkManagerProperties
        {
            public uint State { get; set; }
            public ObjectPath[] Devices { get; set; }
            public ObjectPath[] ActiveConnections { get; set; }
        }

        [DBusInterface("org.freedesktop.NetworkManager.Device")]
        public interface IDevice : IDBusObject
        {
            Task<T> GetAsync<T>(string prop);
            Task<DeviceProperties> GetAllAsync();
            Task SetAsync(string prop, object val);
            Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
        }

        [Dictionary]
        public class DeviceProperties
        {
            public string Interface { get; set; }
            public uint DeviceType { get; set; }
            public uint State { get; set; }
        }

        [DBusInterface("org.freedesktop.NetworkManager.Device.Wireless")]
        public interface IWireless : IDBusObject
        {
            Task RequestScanAsync(IDictionary<string, object> options);
        }

        [DBusInterface("org.freedesktop.NetworkManager.Settings")]
        public interface ISettings : IDBusObject
        {
            Task<ObjectPath[]> ListConnectionsAsync();
            Task<ObjectPath> AddConnectionAsync(IDictionary<string, IDictionary<string, object>> settings);
        }

        [DBusInterface("org.freedesktop.NetworkManager.Settings.Connection")]
        public interface ISettingsConnection : IDBusObject
        {
            Task<IDictionary<string, IDictionary<string, object>>> GetSettingsAsync();
            Task UpdateAsync(IDictionary<string, IDictionary<string, object>> settings);
        }
    }
}
