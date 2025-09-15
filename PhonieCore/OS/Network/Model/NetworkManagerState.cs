namespace PhonieCore.OS.Network.Model
{
    public enum NetworkManagerState : uint
    {
        Unknown = 0,          // Status unbekannt
        Asleep = 10,          // NM ist inaktiv
        Disconnected = 20,    // Kein Device verbunden
        Disconnecting = 30,   // Aktive Verbindung wird getrennt
        Connecting = 40,      // Wird verbunden
        ConnectedLocal = 50,  // Verbunden, aber keine Internet-Route
        ConnectedSite = 60,   // Verbunden, nur eingeschränkter Zugriff (z. B. LAN)
        ConnectedGlobal = 70  // Voll verbunden mit Internet
    }
}
