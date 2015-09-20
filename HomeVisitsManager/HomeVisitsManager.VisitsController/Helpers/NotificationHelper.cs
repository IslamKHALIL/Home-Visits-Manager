using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using System.IO;
using Microsoft.WindowsAzure.Storage;

namespace HomeVisitsManager.VisitsController.Helpers
{
    public class NotificationHelper
    {
        // Create notification
        public static async Task NotifyOwnerAsync(string imageUrl)
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                var urlEncoded = System.Net.WebUtility.UrlEncode(imageUrl);

                // create the notification xml
                ToastTemplateType toastTemplate = ToastTemplateType.ToastImageAndText01;
                XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);
                XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
                toastTextElements[0].AppendChild(toastXml.CreateTextNode("Visitor"));
                XmlNodeList toastImageAttributes = toastXml.GetElementsByTagName("image");
                ((XmlElement)toastImageAttributes[0]).SetAttribute("src", imageUrl);
                ((XmlElement)toastImageAttributes[0]).SetAttribute("alt", "blue");
                IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
                ((XmlElement)toastNode).SetAttribute("duration", "long");
                ((XmlElement)toastNode).SetAttribute("launch", "{\"type\":\"toast\",\"param1\":\"" + urlEncoded + "\"}");
                // ((XmlElement)toastNode).SetAttribute("launch", "{\"type\":\"toast\",\"param1\":\"" + urlEncoded + "\",\"param2\":\"\"}");

                // Send notification to WNS
                await SendNotificationAsync(toastXml.GetXml().ToString());
            }
        }

        public static async Task<string> UploadImageAsync(Stream imageStream)
        {
            CloudStorageAccount account = new CloudStorageAccount(
            useHttps: true,
            storageCredentials: new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(
                    accountName: Constants.StorageAccountName,
                    keyValue: Constants.StorageAccessKey)
                    );

            try
            {
                var blobClient = account.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference("visitors");
                var blob = container.GetBlockBlobReference(Guid.NewGuid().ToString() + ".jpg");
                using (var stream = imageStream.AsInputStream())
                {
                    await blob.UploadFromStreamAsync(stream);
                }
                return blob.Uri.ToString();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // Post notification
        private static async Task SendNotificationAsync(string notificationXML)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var request = new HttpRequestMessage(new HttpMethod("POST"), new Uri("https://login.live.com/accesstoken.srf"));
                    request.Content = new HttpStringContent(string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=notify.windows.com", Constants.StoreClienId, Constants.StoreClientSecret), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
                    var res = await client.SendRequestAsync(request);
                    if (res.IsSuccessStatusCode)
                    {
                        var tokenJson = await res.Content.ReadAsStringAsync();
                        var token = JsonConvert.DeserializeObject<Token>(tokenJson);

                        request = new HttpRequestMessage(new HttpMethod("POST"), new Uri(Constants.OwnerNotificationChannel));
                        request.Content = new HttpStringContent(notificationXML, Windows.Storage.Streams.UnicodeEncoding.Utf8, "text/xml");
                        (request.Content as HttpStringContent).Headers.ContentLength = Convert.ToUInt64(notificationXML.Length);
                        request.Headers.Authorization = new HttpCredentialsHeaderValue(token.TokenType, token.AccessToken);
                        request.Headers.Add("X-WNS-Type", "wns/toast");
                        await client.SendRequestAsync(request);
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }
    }

    public class Token
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        [JsonProperty("token_type")]
        public string TokenType { get; set; }
    }
}
