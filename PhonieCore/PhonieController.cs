using System.Threading;
using System.Threading.Tasks;
using PhonieCore.Logging;

namespace PhonieCore
{
    public static class PhonieController
    {
        public static async Task Run(CancellationToken cancellationToken)
        {
            RfidReader.NewCardDetected += NewCardDetected;

            await Player.SetVolume(50);
            await Player.Play("/media/start.mp3");

            await RfidReader.DetectCards(cancellationToken);

            await Player.Play("/media/shutdown.mp3");
        }

        private static void NewCardDetected(string uid)
        {
            Logger.Log($"new card detected: {uid}");

            Player.ProcessFolder(uid).Wait();

        }
    }
}
