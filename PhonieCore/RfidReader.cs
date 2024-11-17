using System;
using System.Device.Gpio;
using System.Device.Spi;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.Mfrc522;
using PhonieCore.Logging;

namespace PhonieCore
{
    // https://www.nuget.org/packages/nanoFramework.IoT.Device.Mfrc522/

    internal static class RfidReader
    {
        public static event Action<string> NewCardDetected;
        private static string _currentId = string.Empty;

        public static async Task DetectCards(CancellationToken cancellationToken)
        {
            Logger.Log("Starting NFC Reader");

            using var gpioController = new GpioController();
            var pinReset = 22;

            SpiConnectionSettings connection = new(0, 0)
            {
                ClockFrequency = 1_000_000
            };

            using var spi = SpiDevice.Create(connection);
            MfRc522 mfrc522 = new(spi, pinReset, gpioController, false);

            while (!cancellationToken.IsCancellationRequested)
            {
                await mfrc522.DetectCard();
            }

            Logger.Log("Stopping NFC Reader");
        }

        private static async Task  DetectCard(this MfRc522 mfrc522)
        {
            var res = mfrc522.ListenToCardIso14443TypeA(out var card, TimeSpan.FromMilliseconds(10));
            if (!res)
            {
                await Task.Delay(500);
                return;
            }

            var id = BitConverter.ToString(card.NfcId);
            if (id.Equals(_currentId))
            {
                return;
            }

            _currentId = id;
            OnNewCardFound(id);

            await Task.Delay(500);
        }

        private static void OnNewCardFound(string id)
        {
            NewCardDetected?.Invoke(id);
        }
    }
}
