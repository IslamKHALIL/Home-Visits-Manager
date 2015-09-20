using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace HomeVisitsManager.VisitsController.Helpers
{
    public class OxfordHelper
    {
        private static FaceServiceClient _Client = new FaceServiceClient(Constants.OxfordKey);

        public static async Task<AuthenticationResult> IdentifyAsync(string imageUrl)
        {
            try
            {
                var faces = await _Client.DetectAsync(imageUrl);

                if (faces.Length > 0)
                {
                    var identifyResults = await _Client.IdentifyAsync(Constants.OwnerGroupId, faces.Select(f => f.FaceId).ToArray());

                    if (identifyResults.Any(r => r.Candidates.Any(c => c.PersonId == Constants.OwnerPersonId)))
                        return AuthenticationResult.IsOwner;
                    else
                        return AuthenticationResult.Unkown;
                }
                return AuthenticationResult.None;
            }
            catch
            {
                return AuthenticationResult.Unkown;
            }
        }

        private static async Task TrainAsync()
        {
            List<Stream> streams = new List<Stream>();
            for (int i = 1; i < 7; i++)
            {
                string filesString = @"ms-appx:///Assets/Training (" + i + ").jpg";
                var randomAcessStream = await RandomAccessStreamReference.CreateFromUri(new Uri(filesString)).OpenReadAsync();
                var stream = randomAcessStream.AsStream();
                streams.Add(stream);
            }
            var faces = new List<Face>();
            foreach (var item in streams)
            {
                var dfaces = await _Client.DetectAsync(item);
                faces.AddRange(dfaces);
            }

            await _Client.CreatePersonGroupAsync(Constants.OwnerGroupId, Constants.OwnerName);
            await _Client.CreatePersonAsync(Constants.OwnerGroupId, faces.Select(f => f.FaceId).ToArray(), Constants.OwnerName);
            await _Client.TrainPersonGroupAsync(Constants.OwnerGroupId);
        }
    }

    public enum AuthenticationResult
    {
        None,
        IsOwner,
        Unkown
    }
}
