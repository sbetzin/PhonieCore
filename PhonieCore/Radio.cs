using System;

namespace PhonieCore
{
    public class Radio
    {
        private readonly Player _player;

        public Radio()
        {
            _player = new Player();

            RfidReader.NewCardDetected += NewCardDetected;
            RfidReader.DetectCards();
        }

        private void NewCardDetected(string uid)
        {
            Console.WriteLine($"New card: " + uid);
            _player.ProcessFolder(uid).Wait();

        }
    }
}
