using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Spi;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.Mfrc522;
using Iot.Device.Rfid;

namespace PhonieCore
{
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

            bool res;
            Data106kbpsTypeA card;
            do
            {
                res = mfrc522.ListenToCardIso14443TypeA(out card, TimeSpan.FromSeconds(2));

                if (res)
                {
                    
                    Console.WriteLine("card");
                }
                else
                {
                    Console.WriteLine("No card.");
                }
                Thread.Sleep(res ? 0 : 1000);
            }
            while (!res);
        }
    }
}
