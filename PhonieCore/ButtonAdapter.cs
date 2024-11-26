using System;
using System.Device.Gpio;
using System.Threading.Tasks;
using PhonieCore.Logging;

namespace PhonieCore
{
    public class ButtonAdapter(PlayerState state)
    {
        public async Task WatchButton()
        {
            try
            {
                Logger.Log("Start Button Watcher");
                var controller = new GpioController();

                // GPIO 26 als Eingang mit internem Pull-Down-Widerstand öffnen
                var pinNumber = 26;
                controller.OpenPin(pinNumber, PinMode.InputPullDown);

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
            Logger.Log($"Pin changed: {pinvaluechangedeventargs.ChangeType}");
        }
    }
}
