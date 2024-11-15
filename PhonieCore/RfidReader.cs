using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Spi;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.Mfrc522;
using Iot.Device.Rfid;
using UnitsNet;

namespace PhonieCore
{
    // https://www.nuget.org/packages/nanoFramework.IoT.Device.Mfrc522/

    internal class RfidReader
    {
        public static void WaitForCard()
        {
            using var gpioController = new GpioController();
            var pinReset = 21;
            
            SpiConnectionSettings connection = new(0, 0)
            {
                ClockFrequency = 10_000_000
            };

            using var spi = SpiDevice.Create(connection);
            MfRc522 mfrc522 = new(spi, pinReset, gpioController, false);

            while (true)
            {
                var res = mfrc522.ListenToCardIso14443TypeA(out var card, TimeSpan.FromSeconds(1));

                if (res)
                {

                    //Console.WriteLine("card");
                    var id = BitConverter.ToString(card.NfcId);

                    Console.WriteLine(id);
                    Thread.Sleep(1000);
                }
                else
                {
                    //Console.WriteLine("No card.");
                }

            }
        }
    }
}
