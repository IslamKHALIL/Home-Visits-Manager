using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.System.Threading;

namespace HomeVisitsManager.VisitsController.Controllers
{
    public class MotorController
    {
        private static IAsyncAction workItemThread;

        public static void PWM(int pinNumber)
        {
            var stopwatch = Stopwatch.StartNew();
            var pin = GpioController.GetDefault().OpenPin(pinNumber);
            pin.SetDriveMode(GpioPinDriveMode.Output);

            workItemThread = Windows.System.Threading.ThreadPool.RunAsync(
                 (source) =>
                 {
                     // setup, ensure pins initialized
                     ManualResetEvent mre = new ManualResetEvent(false);
                     mre.WaitOne(1500);

                     ulong pulseTicks = ((ulong)(Stopwatch.Frequency) / 1000) * 2;
                     ulong delta;
                     var startTime = stopwatch.ElapsedMilliseconds;
                     while (stopwatch.ElapsedMilliseconds - startTime <= 300)
                     {
                         pin.Write(GpioPinValue.High);
                         ulong starttick = (ulong)(stopwatch.ElapsedTicks);
                         while (true)
                         {
                             delta = (ulong)(stopwatch.ElapsedTicks) - starttick;
                             if (delta > pulseTicks) break;
                         }
                         pin.Write(GpioPinValue.Low);
                         starttick = (ulong)(stopwatch.ElapsedTicks);
                         while (true)
                         {
                             delta = (ulong)(stopwatch.ElapsedTicks) - starttick;
                             if (delta > pulseTicks * 10) break;
                         }
                     }
                 }, WorkItemPriority.High);
        }
    }
}