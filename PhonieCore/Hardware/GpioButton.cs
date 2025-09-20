using System;
using System.Device.Gpio;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace PhonieCore.Hardware
{
    public static class GpioButton
    {
        public static async Task RunAsync(
            int pin,
            PinMode mode,                 // z.B. InputPullUp (Taster nach GND)
            TimeSpan debounce,            // z.B. TimeSpan.FromMilliseconds(30)
            bool pressedIsLow,            // true: LOW=gedrückt (typisch bei PullUp)
            Action onPressed,
            Action onReleased,
            CancellationToken ct)
        {
            using var gpio = new GpioController();
            gpio.OpenPin(pin, mode);

            // 1) Event-Stream aus den HW-Flanken erzeugen
            var changes = Observable.Create<PinValueChangedEventArgs>(observer =>
            {
                PinChangeEventHandler handler = (_, e) => observer.OnNext(e);
                gpio.RegisterCallbackForPinValueChangedEvent(
                    pin,
                    PinEventTypes.Rising | PinEventTypes.Falling,
                    handler);

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
    }
}