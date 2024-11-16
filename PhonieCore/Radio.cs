using System;
using System.Net.WebSockets;

namespace PhonieCore
{
    public class Radio
    {
        private readonly Player _player;

        public Radio()
        {
            _player = new Player();

            RfidReader.NewCardDetected += NewCardDetected;

            RfidReader.WaitForCard();
        }

        private void NewCardDetected(string uid)
        {
            Console.WriteLine($"New card: " + uid);
            var file = new string[] { "/media/test.mp3" };
            _player.Play(file).Wait();

        }

        private void HandleNewCardDetected(string uid)
        {
            
            _player.ProcessFolder(uid);
        }

    }
}
