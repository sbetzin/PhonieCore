using System;
using System.Device.Gpio;
using System.Device.Spi;
using System.Threading;
using Iot.Device.Mfrc522;

namespace PhonieCore
{
    // https://www.nuget.org/packages/nanoFramework.IoT.Device.Mfrc522/

    internal class RfidReader
    {
        public static event Action<string> NewCardDetected;

        public static void WaitForCard()
        {
            using var gpioController = new GpioController();
            var pinReset = 22;
            var currentId = string.Empty;

            SpiConnectionSettings connection = new(1, 0)
            {
                ClockFrequency = 1_000_000
            };

            using var spi = SpiDevice.Create(connection);
            MfRc522 mfrc522 = new(spi, pinReset, gpioController, false);

            while (true)
            {
                currentId = DetectCard(mfrc522, currentId);
            }
        }

        private static string DetectCard(MfRc522 mfrc522, string currentId)
        {
            var res = mfrc522.ListenToCardIso14443TypeA(out var card, TimeSpan.FromSeconds(2));
            if (!res)
            {
                return currentId;
            }
                
            var id = BitConverter.ToString(card.NfcId);

            if (id.Equals(currentId))
            {
                return currentId;
            }
            currentId = id;
            OnNewCardFound(id);

            Console.WriteLine(id);
            Thread.Sleep(1000);
            return currentId;
        }

        protected static void OnNewCardFound(string id)
        {
            NewCardDetected?.Invoke(id);
        }
    }
}
