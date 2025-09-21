using PhonieCore.Logging;
using PhonieCore.OS.Network.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace PhonieCore.OS
{
    public partial class NetworkManagerAdapter
    {
        private INetworkManager _networkManager;
        private ISettings _settings;
        private Connection _bus;
        private string _ifname;
        public NetworkManagerState CurrentState;

        public event Action<NetworkManagerState> NetworkStatusChanged;

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

                await RefreshCurrentState();
                Logger.Log($"Initial network state: {CurrentState}");

                await _networkManager.WatchStateChangedAsync(NetworkStateChange);
            }
            catch (Exception e)
            {
                Logger.Error($"Could not connect to network manager: {e.Message}");
            }
        }

        private async Task RefreshCurrentState()
        {
            var state = await _networkManager.GetAsync<uint>("State");
            CurrentState = (NetworkManagerState)state;
        }

        private void NetworkStateChange(uint newState)
        {
            CurrentState = (NetworkManagerState)newState;

            Logger.Log($"network state changed: {CurrentState}");

            OnNetworkStatusChanged(CurrentState);
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
                Logger.Error($"Error request a network rescan for {_ifname}: {e.Message}");
            }
        }

        public async Task EnsureWifiProfileAsync(string name, string ssid, string psk, bool hidden = false)
        {
            try
            {
                var existing = await FindConnectionByIdAsync(name);
                var settings = BuildWifiSettings(name, ssid, psk, hidden);

                if (existing == null)
                {
                    Logger.Log($"creating wifi profile {name} - {ssid}");
                    await _settings.AddConnectionAsync(settings);
                }
                else
                {
                    Logger.Log($"updatting wifi profile {name} - {ssid}");
                    var settingsConnection = _bus.CreateProxy<ISettingsConnection>("org.freedesktop.NetworkManager", existing.Value);
                    await settingsConnection.UpdateAsync(settings);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Could not ensure wifi profile with {name}: {e.Message}");
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

        protected virtual void OnNetworkStatusChanged(NetworkManagerState state)
        {
            NetworkStatusChanged?.Invoke(state);
        }
    }
}
