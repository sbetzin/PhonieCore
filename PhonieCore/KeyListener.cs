//using Unosquare.RaspberryIO;
//using Unosquare.RaspberryIO.Abstractions;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;

//namespace PhonieCore
//{
//    public class KeyListener
//    {
//        public delegate void KeyPressedHandler(int key);
//        public event KeyPressedHandler OnKeyPressed;
//        public delegate void KeyReleasedHandler(int key);
//        public event KeyReleasedHandler OnKeyReleased;

//        Dictionary<BcmPin, bool> _buttonState;

//        public KeyListener()
//        {
//            Task.Run(WatchKeys);
//        }   

//        public void WatchKeys()
//        {
//            _buttonState = new Dictionary<BcmPin, bool>();
//            InitGpio(BcmPin.Gpio26);
//            _buttonState.Add(BcmPin.Gpio26, false);
//            InitGpio(BcmPin.Gpio06);
//            _buttonState.Add(BcmPin.Gpio06, false);
//            InitGpio(BcmPin.Gpio05);
//            _buttonState.Add(BcmPin.Gpio05, false);
//            InitGpio(BcmPin.Gpio16);
//            _buttonState.Add(BcmPin.Gpio16, false);

//            while (true)
//            {
//                Thread.Sleep(100);
//                foreach (var pin in _buttonState.Keys)
//                {
//                    CheckButton(pin, _buttonState); 
//                }
//            }
//        }  
        
//        private void CheckButton(BcmPin pin, Dictionary<BcmPin, bool> buttonState)
//        {
//            if (!Pi.Gpio[pin].Read() && !buttonState[pin])
//            {
//                buttonState[pin] = true;
//                OnKeyPressed?.Invoke((int)pin);
//            }

//            if (Pi.Gpio[pin].Read() && buttonState[pin])
//            {
//                OnKeyReleased?.Invoke((int)pin);
//                buttonState[pin] = false;
//            }
//        }

//        private void InitGpio(BcmPin pin)
//        {
//            var gpio = Pi.Gpio[pin];
//            gpio.InputPullMode = GpioPinResistorPullMode.PullUp;
//            gpio.PinMode = GpioPinDriveMode.Input;
//        }
//    }
//}
