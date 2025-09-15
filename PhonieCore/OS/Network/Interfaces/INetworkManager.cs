using System;
using System.Threading.Tasks;
using Tmds.DBus;

namespace PhonieCore.OS.Network.Interfaces
{
    [DBusInterface("org.freedesktop.NetworkManager")]
    internal interface INetworkManager : IDBusObject
    {
        Task<uint> GetStateAsync();
        Task<IDisposable> WatchStateChangedAsync(Action<(uint newState, uint oldState, uint reason)> handler);
    }
}
