using Microsoft.AspNet.SignalR;

namespace VisitsMangerServer
{
    public class DoorHub : Hub
    {
        public void SendMessageToDoor()
        {
            Clients.All.GetMessage();
        }
    }
}