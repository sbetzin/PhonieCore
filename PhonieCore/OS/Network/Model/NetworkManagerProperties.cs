using Tmds.DBus;

namespace PhonieCore.OS
{
    public partial class NetworkManagerAdapter
    {
        [Dictionary]
        public class NetworkManagerProperties
        {
            public uint State { get; set; }
            public ObjectPath[] Devices { get; set; }
            public ObjectPath[] ActiveConnections { get; set; }
        }
    }
}
