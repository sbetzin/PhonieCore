using Tmds.DBus;

namespace PhonieCore.OS
{
    public partial class NetworkManagerAdapter
    {
        [Dictionary]
        public class DeviceProperties
        {
            public string Interface { get; set; }
            public uint DeviceType { get; set; }
            public uint State { get; set; }
        }
    }
}
