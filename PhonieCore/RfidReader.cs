﻿using System;
using System.Device.Gpio;
using System.Device.Spi;
using System.Threading;
using Iot.Device.Mfrc522;

namespace PhonieCore
{
    // https://www.nuget.org/packages/nanoFramework.IoT.Device.Mfrc522/

    internal static class RfidReader
    {
        public static event Action<string> NewCardDetected;
        private static string _currentId = string.Empty;

        public static void DetectCards()
        {
            using var gpioController = new GpioController();
            var pinReset = 22;

            SpiConnectionSettings connection = new(0, 0)
            {
                ClockFrequency = 1_000_000
            };

            using var spi = SpiDevice.Create(connection);
            MfRc522 mfrc522 = new(spi, pinReset, gpioController, false);

            while (true)
            {
                mfrc522.DetectCard();
            }
        }

        private static void DetectCard(this MfRc522 mfrc522)
        {
            var res = mfrc522.ListenToCardIso14443TypeA(out var card, TimeSpan.FromSeconds(2));
            if (!res)
            {
                return;
            }

            var id = BitConverter.ToString(card.NfcId);
            if (id.Equals(_currentId))
            {
                return;
            }

            _currentId = id;
            OnNewCardFound(id);

            Thread.Sleep(1000);
        }

        private static void OnNewCardFound(string id)
        {
            NewCardDetected?.Invoke(id);
        }
    }
}
