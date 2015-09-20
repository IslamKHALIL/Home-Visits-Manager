using Microsoft.AspNet.SignalR.Client;
using System;
using Windows.Networking.PushNotifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace HomeVisitsManager.HouseOwner
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        HubConnection hubConnection = new HubConnection("http://localhost:51109/");
        IHubProxy DoorHubProxy = null;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
            IHubProxy DoorHubProxy = hubConnection.CreateHubProxy("DoorHub");
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.

            try
            {
                // To get the notification channel
                PushNotificationChannel channel = null;
                channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                Constants.OwnerNotificationChannel = channel.Uri.ToString();
                await hubConnection.Start();
            }
            catch
            {
            }
        }

        private async void EnterButton_Click(object sender, RoutedEventArgs e)
        {
            await DoorHubProxy.Invoke("SendMessageToDoor");
            VisitorImage.Source = null;
        }

        private void RejectButton_Click_1(object sender, RoutedEventArgs e)
        {
            VisitorImage.Source = null;
        }
    }
}
