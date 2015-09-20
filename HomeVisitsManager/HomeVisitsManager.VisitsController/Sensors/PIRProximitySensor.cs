using System;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace HomeVisitsManager.VisitsController.Sensors
{
    /// <summary>
    /// This class helps to manage and control PIR sensor
    /// To know more about PIR: https://learn.adafruit.com/pir-passive-infrared-proximity-motion-sensor/overview
    /// </summary>
    public class PIRProximitySensor
    {
        /// <summary>
        /// Initialize the connected pin of the GPIO as an input pin
        /// </summary>
        /// <param name="pinNumber">GPIO pin number from the Raspberry PI, to which the sensor's output pin is connected</param>
        public PIRProximitySensor(int pinNumber)
        {
            var Controller = GpioController.GetDefault();
            if (Controller == null)
                throw new Exception("No GPIO available");

            Pin = Controller.OpenPin(pinNumber);
            Pin.SetDriveMode(GpioPinDriveMode.Input);
            Run();
        }

        /// <summary>
        /// Fired when motion detected by the PIR
        /// </summary>
        public event Action<int> MotionDetected;

        /// <summary>
        /// Enable/Disable read from the sensor
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// PIR pin
        /// </summary>
        private GpioPin Pin { get; set; }

        /// <summary>
        /// Start sensor's reading loop
        /// </summary>
        private async void Run()
        {
            await Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (IsActive)
                    {
                        var value = Pin.Read();
                        if (value == GpioPinValue.High)
                            MotionDetected?.Invoke(Pin.PinNumber);
                        await Task.Delay(1000);
                    }
                }
            });
        }
    }
}
