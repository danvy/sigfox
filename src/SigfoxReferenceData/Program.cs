using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using SigfoxLib;

namespace SigfoxReferenceData
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new CloudBlobClient(new StorageUri(new Uri("https://sigfox.blob.core.windows.net/")), new StorageCredentials("sigfox", "nyXvyZezWGZE5KX9eUYVJ7v/XHVG/l8uBB0cYr7zt0sGXTNq0W6JggC6jxDQSySQXIongfoXqrtA2CaXomc/Vg=="));
            var container = client.GetContainerReference("locations");
            if (container.Exists())
                container.Delete();
            container.CreateIfNotExists();
            DeviceLocations.Init();
            foreach (var device in DeviceLocations.Instance)
            {
                Console.WriteLine(string.Format("Device {0}", device.DeviceId));
                var blob = container.GetBlockBlobReference(device.DeviceId);
                var json = JsonConvert.SerializeObject(device);
                using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(json)))
                {
                    blob.UploadFromStream(stream);
                }
            }
        }
    }
}
