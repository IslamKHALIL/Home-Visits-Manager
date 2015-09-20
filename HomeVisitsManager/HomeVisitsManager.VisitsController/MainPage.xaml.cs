using HomeVisitsManager.VisitsController.Sensors;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.ProjectOxford.Face;
using HomeVisitsManager.VisitsController.Helpers;
using System.Threading.Tasks;
using HomeVisitsManager.VisitsController.Controllers;
using Microsoft.AspNet.SignalR.Client;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HomeVisitsManager.VisitsController
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static FaceServiceClient _Client = new FaceServiceClient(Constants.OxfordKey);
        private static HubConnection _HubConnection = new HubConnection("http://localhost:51109/");
        private static PIRProximitySensor _Sensor = new PIRProximitySensor(18);


        public MainPage()
        {
            this.InitializeComponent();
            IHubProxy DoorHubProxy = _HubConnection.CreateHubProxy("DoorHub");
            DoorHubProxy.On("GetMessage", async () =>
            {
                _Sensor.IsActive = false;
                MotorController.PWM(26);
                await Task.Delay(20000);
                _Sensor.IsActive = true;
            });
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // await _HubConnection.Start();

            var cam = new MediaCapture();

            await cam.InitializeAsync(new MediaCaptureInitializationSettings()
            {
                MediaCategory = MediaCategory.Media,
                StreamingCaptureMode = StreamingCaptureMode.Video
            });

            _Sensor.MotionDetected += async (int pinNum) =>
            {
                await Task.Factory.StartNew(async () =>
                {
                    _Sensor.IsActive = false;
                    var stream = new InMemoryRandomAccessStream();
                    await cam.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);
                    stream.Seek(0);
                    var imageStream = stream.AsStream();
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
                            await NotificationHelper.NotifyOwnerAsync(imageUrl);
                            break;

                        case AuthenticationResult.None:
                        default:
                            break;
                    }
                    _Sensor.IsActive = true;
                });
            };
        }
    }
}
