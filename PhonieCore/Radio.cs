using System;
using System.Net.WebSockets;

namespace PhonieCore
{
    public class Radio
    {
        private readonly Player _player;

        public Radio()
        {
            //Unosquare.RaspberryIO.Pi.Init<BootstrapWiringPi>();

            _player = new Player();

            //var keyListener = new KeyListener();
            //keyListener.OnKeyPressed += HandleKeyPressed;
            //keyListener.OnKeyReleased += HandleKeyReleased;

            Console.WriteLine("Playing test");
            var file = new string[] { "/media/test.mp3" };
            _player.Play(file);

            //RfidReader.WaitForCard();
            //rfid.CardDetected += HandleCardDetected;
            //rfid.NewCardDetected += HandleNewCardDetected;


        }

        private void HandleCardDetected(string uid)
        {
        }

        private void HandleNewCardDetected(string uid)
        {
            Console.WriteLine($"New card: " + uid);
            _player.ProcessFolder(uid);
        }

        private void HandleKeyPressed(int key)
        {
            Console.WriteLine("Pressed: " + key);

            switch (key)
            {
                case 6:
                    _player.Pause();
                    break;
                case 16:
                    _player.Play();
                    break;
                case 5:
                    _player.IncreaseVolume();
                    break;
                case 26:
                    _player.DecreaseVolume();
                    break;
            }
        }

        private void HandleKeyReleased(int key)
        {
            Console.WriteLine("Released: " + key);
        }
    }
}
