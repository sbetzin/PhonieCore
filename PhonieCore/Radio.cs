using System.Threading;
using PhonieCore.Logging;

namespace PhonieCore
{
    public static class Radio
    {

        public static void Start(CancellationToken cancellationToken)
        {
            RfidReader.NewCardDetected += NewCardDetected;

            Player.SetVolume(80).Wait(cancellationToken);

            RfidReader.DetectCards(cancellationToken);
        }

        private static void NewCardDetected(string uid)
        {
            Logger.Log($"new card detected: {uid}");

            Player.ProcessFolder(uid).Wait();

        }
    }
}
