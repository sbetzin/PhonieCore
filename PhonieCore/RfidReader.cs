using System;
using System.Device.Gpio;
using System.Device.Spi;
using System.Threading.Tasks;
using Iot.Device.Mfrc522;
using PhonieCore.Logging;

namespace PhonieCore
{
    // https://www.nuget.org/packages/nanoFramework.IoT.Device.Mfrc522/

    internal static class RfidReader
    {
        public static event Action<string> NewCardDetected;


        public static async Task DetectCards(PlayerState state)
        {
            Logger.Log($"Starting NFC Reader BusId={state.BusId}, ChipSelectLine={state.ChipSelectLine}");

            using var gpioController = new GpioController();
            var pinReset = 22;

            SpiConnectionSettings connection = new(state.BusId, state.ChipSelectLine)
            {
                ClockFrequency = 1_000_000
            };

            using var spi = SpiDevice.Create(connection);
            MfRc522 mfrc522 = new(spi, pinReset, gpioController, false);

            while (!state.CancellationToken.IsCancellationRequested)
            {
                mfrc522.DetectCard();
                await Task.Delay(500);
            }

            Logger.Log("Stopping NFC Reader");
        }

        private static void DetectCard(this MfRc522 mfrc522)
        {
            var res = mfrc522.ListenToCardIso14443TypeA(out var card, TimeSpan.FromMilliseconds(10));
            if (!res)
            {
                return;
            }

            var id = BitConverter.ToString(card.NfcId);

            OnNewCardFound(id);
        }

        private static void OnNewCardFound(string id)
        {
            NewCardDetected?.Invoke(id);
        }
    }
}
