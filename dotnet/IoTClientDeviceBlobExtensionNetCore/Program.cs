using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;


namespace IoTClientDeviceBlobExtensionNetCore
{
    class Program
    {
        static string DeviceConnectionString = "PUT CONNECTION STRING HERE";
        static DeviceClient Client = null;

        static BlobExtension blobClient = null;
        static string blobConfigPropertyName = "configurationBlob";

        static void Main(string[] args)
        { 
            try
            {
                InitClient();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }

        /// <summary>
        /// Init method for Iot hub client device that will download first time the external blob and subscribe on blob changes
        /// </summary>
        public static async Task InitClient()
        {           
            try
            {
                Console.WriteLine("Connecting to hub");
                Client = DeviceClient.CreateFromConnectionString(DeviceConnectionString,
                  TransportType.Mqtt);
                Console.WriteLine("Retrieving twin");
                var twin = await Client.GetTwinAsync();

                //Init Blob extenstion class create it per each blob attachment              
                blobClient = new BlobExtension(Client, blobConfigPropertyName);
                
                //Download first time the blob attachment on a first start
                string bigBlobContent = await blobClient.DownloadBlobAsync(twin.Properties.Desired[blobConfigPropertyName]);

                //Subscribe on event if blob will be changed
                blobClient.BlobPropertyUpdatedEvent += BlobClient_BlobPropertyUpdatedEvent;               

                Console.WriteLine("Twin: {0}", twin.ToJson());
                Console.WriteLine("Big Blob content length: {0}", bigBlobContent.Length.ToString());

            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }            
        }

        private static void BlobClient_BlobPropertyUpdatedEvent(BlobExtension.BlobPropertyUpdatedArgs e, object sender)
        {           
            //Do your things here after extended blob was updated///
            ////
            //////
            ////////
            //////////          
        }
        
    }
}
