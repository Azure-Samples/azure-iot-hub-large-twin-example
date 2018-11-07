using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;


namespace IoTClientDeviceBlobExtensionNetCore
{
    class Program
    {
        static string DeviceConnectionString = "Put Device Connection string here";
        static DeviceClient Client = null;

        static BlobExtension blobClient = null;
        static string blobConfigPropertyName = "configurationBlob";

        static void Main(string[] args)
        { 
            try
            {
                InitClient()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
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

                //Init Blob extenstion class create it per each blob attachment              
                blobClient = new BlobExtension(Client, blobConfigPropertyName);
                
                //Subscribe on event if blob will be changed
                blobClient.BlobPropertyUpdatedEvent += BlobClient_BlobPropertyUpdatedEvent;
                await blobClient.GetInitialTwin();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }            
        }

        private static void BlobClient_BlobPropertyUpdatedEvent(BlobExtension.BlobPropertyUpdatedArgs e, object sender)
        {           
            ProcessContent(e.BlobContent);
        }

        private static void ProcessContent(string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                Console.WriteLine(content);
            }
        }
        
    }
}
