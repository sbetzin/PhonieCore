using System;
using System.Device.Gpio;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using PhonieCore.Logging;

namespace PhonieCore.Hardware
{
    public static class GpioButton
    {
        public static async Task RunAsync(int pin, PinMode mode, TimeSpan debounce, bool pressedIsLow, Action onPressed, Action onReleased, CancellationToken ct)
        {
            try
            {
                Logger.Log($"wating button {pin} for {debounce.TotalMilliseconds} ms debounce");
                using var gpio = new GpioController();
                gpio.OpenPin(pin, mode);

                var changes = Observable.Create<PinValueChangedEventArgs>(observer =>
                {
                    PinChangeEventHandler handler = (_, e) => observer.OnNext(e);
                    gpio.RegisterCallbackForPinValueChangedEvent(pin, PinEventTypes.Rising | PinEventTypes.Falling, handler);

                    return Disposable.Create(() =>
                    {
                        try { gpio.UnregisterCallbackForPinValueChangedEvent(pin, handler); } catch { }
                    });
                });

                // 2) Debounce (in RX = Throttle) + "Settle-Read"
                using var sub = changes
                    .Throttle(debounce)           // lässt nur eine Änderung nach Ruhezeit durch
                    .Select(_ => gpio.Read(pin))  // nach Wartezeit stabilen Wert lesen
                    .Subscribe(v =>
                    {
                        if (v == PinValue.Low == pressedIsLow) onPressed?.Invoke();
                        else onReleased?.Invoke();
                    });

                try { await Task.Delay(Timeout.Infinite, ct); }
                catch (OperationCanceledException) { /* beenden */ }
            }
            catch (Exception ex)
            {
                Logger.Error("Fehler beim registrieren der Button Events", ex);
            }
           
        }
    }
}