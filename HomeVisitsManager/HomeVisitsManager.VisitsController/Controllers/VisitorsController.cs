using HomeVisitsManager.VisitsController.Sensors;
using Microsoft.ProjectOxford.Face;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using System.IO;
using Microsoft.ProjectOxford.Face.Contract;
using HomeVisitsManager.VisitsController.Helpers;
using Microsoft.AspNet.SignalR.Client;

namespace HomeVisitsManager.VisitsController.Controllers
{
    public class VisitorsController
    {
        #region Constructors

        public VisitorsController()
        {
            IHubProxy DoorHubProxy = _HubConnection.CreateHubProxy("DoorHub");
            DoorHubProxy.On("GetMessage", async () =>
            {
                _Sensor.IsActive = false;
                MotorController.PWM(26);
                await Task.Delay(20000);
                _Sensor.IsActive = true;
            });
            Run();
        }

        #endregion

        #region Private fields

        private static FaceServiceClient _Client = new FaceServiceClient(Constants.OxfordKey);
        private static HubConnection _HubConnection = new HubConnection("http://localhost:51109/");
        private static PIRProximitySensor _Sensor = new PIRProximitySensor(18);

        #endregion

        #region Methods

        private async void Run()
        {
            await _HubConnection.Start();

            var cam = new MediaCapture();

            await cam.InitializeAsync(new MediaCaptureInitializationSettings()
            {
                MediaCategory = MediaCategory.Media,
                StreamingCaptureMode = StreamingCaptureMode.Video
            });

            _Sensor.MotionDetected += async (int pinNum) =>
            {
                var stream = new InMemoryRandomAccessStream();
                Stream imageStream = null;
                try
                {
                    await Task.Factory.StartNew(async () =>
                    {
                        _Sensor.IsActive = false;
                        await cam.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);
                        stream.Seek(0);
                        imageStream = stream.AsStream();
                        imageStream.Seek(0, SeekOrigin.Begin);
                        string imageUrl = await NotificationHelper.UploadImageAsync(imageStream);

                        switch (await OxfordHelper.IdentifyAsync(imageUrl))
                        {
                            case AuthenticationResult.IsOwner:
                                // open the door
                                MotorController.PWM(26);
                                break;

                            case AuthenticationResult.Unkown:
                                // send notification to the owner
                                NotificationHelper.NotifyOwnerAsync(imageUrl);
                                break;

                            case AuthenticationResult.None:
                            default:
                                break;
                        }
                        _Sensor.IsActive = true;
                    });
                }
                finally
                {
                    if (stream != null)
                        stream.Dispose();
                    if (imageStream != null)
                        imageStream.Dispose();
                }
            };
        }

        #endregion
    }
}
