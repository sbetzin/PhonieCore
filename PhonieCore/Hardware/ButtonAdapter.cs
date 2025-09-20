using System;
using System.Device.Gpio;
using System.Threading.Tasks;
using PhonieCore.Logging;

namespace PhonieCore.Hardware
{
    public class ButtonAdapter(PlayerState state)
    {
        public async Task WatchButton(PinMode pinmode, int pinNumber)
        {
            try
            {
                Logger.Log("Start Button Watcher");
                var controller = new GpioController();
                controller.OpenPin(pinNumber, pinmode);

                controller.RegisterCallbackForPinValueChangedEvent(
                    pinNumber,
                    PinEventTypes.Rising | PinEventTypes.Falling,
                    PinChangedHandler
                );

                while (!state.CancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(500);
                }
            }
            catch (Exception e)
            {
                Logger.Log(e.Message);
            }
        }

        private void PinChangedHandler(object sender, PinValueChangedEventArgs pinvaluechangedeventargs)
        {
            Logger.Log($"Pin changed: {pinvaluechangedeventargs.PinNumber} - {pinvaluechangedeventargs.ChangeType}");
        }
    }
}
