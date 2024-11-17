
using System.Threading;

namespace PhonieCore
{
    public class PlayerState(int busId, int chipSelectLine, string mediaFolder)
    {
        public CancellationToken CancellationToken { get; set; }
        public int Volume { get; set; } = 50;
        public string PlayingTag { get; set; }= string.Empty;
        public string MediaFolder { get; set; } = mediaFolder;
        public int BusId { get; set; } = busId;
        public int ChipSelectLine { get; set; } = chipSelectLine;
    }
}
