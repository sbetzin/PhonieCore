using Unosquare.RaspberryIO.Peripherals;
using System.Text;
using System;
using System.Threading.Tasks;

namespace PhonieCore
{
    public class Rfid
    {
        public delegate void NewCardDetectedHandler(string uid);
        public event NewCardDetectedHandler NewCardDetected;

        public delegate void CardDetectedHandler(string uid);
        public event CardDetectedHandler CardDetected;

        public Rfid()
        {
            Task.Run(WatchRfid);
        }

        public void WatchRfid()
        {
            try
            {
                var reader = new RFIDControllerMfrc522();           

                var lastUid = string.Empty;
                while (true)
                {
                    lastUid = CheckReader(reader, lastUid);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private string CheckReader(RFIDControllerMfrc522 reader, string lastUid)
        {
            if (reader.DetectCard() != RFIDControllerMfrc522.Status.AllOk)
            {
                return lastUid;
            }

            var uidResponse = reader.ReadCardUniqueId();
            if (uidResponse.Status != RFIDControllerMfrc522.Status.AllOk)
            {
                return lastUid;
            }

            var cardUid = uidResponse.Data;
            reader.SelectCardUniqueId(cardUid);

            var currentUid = ByteArrayToString(cardUid);
            if(currentUid != lastUid)
            {
                lastUid = currentUid;
                NewCardDetected?.Invoke(currentUid);
            }
            else
            {
                CardDetected?.Invoke(currentUid);
            }

            return lastUid;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (var b in ba)
            {
                hex.Append($"{b:x2}");
            }
            return hex.ToString();
        }
    }
}
