using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.IO;
using Newtonsoft.Json;

namespace IoTClientDeviceBlobExtensionNetCore
{
    /// <summary>
    /// This class add ability to extended IoT twin json file with link to external azure blob file.
    /// Currently IoT hub twin json have limitation for >4kb
    /// </summary>
    class BlobExtension
    {
        private DeviceClient client = null;
        private string blobConfigPropertyName;

        #region Event initialization to handle blob changes
        public delegate void BlobPropertyUpdated(BlobPropertyUpdatedArgs e, object sender);

        private BlobPropertyUpdated _BlobPropertyUpdatedEvent;

        protected virtual void OnBlobPropertyUpdatedEvent(BlobPropertyUpdatedArgs e, object sender)
        {
            var BlobPropertyUpdatedEvent = this._BlobPropertyUpdatedEvent;
            if (BlobPropertyUpdatedEvent == null)
                return;

            BlobPropertyUpdatedEvent(e, sender);
        }

        public event BlobPropertyUpdated BlobPropertyUpdatedEvent
        {
            add
            {
                this._BlobPropertyUpdatedEvent += value;
                client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, this).ConfigureAwait(true);
            }
            remove
            {
                this._BlobPropertyUpdatedEvent -= value;
                client.SetDesiredPropertyUpdateCallbackAsync(null, this);
            }
        }
        #endregion

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="Client">IoT Hub Client class instance</param>
        /// <param name="BlobConfigPropertyName">A section name where information about the blob will be stored. 
        /// </param>
        public BlobExtension(DeviceClient Client, string BlobConfigPropertyName) {

            blobConfigPropertyName = BlobConfigPropertyName;
            client = Client;
        }
        /// <summary>
        /// Downloading extended blob based on link information from twin json file
        /// </summary>
        /// <param name="ConfigBlobSectionContent">JSON Section content from IOT Hub twin that
        /// contains information about blob extended properties</param>     
        async public Task<string> DownloadBlobAsync(dynamic ConfigBlobSectionContent) {
            string content = String.Empty;
            string sasUri = (string)ConfigBlobSectionContent["uri"];
            CloudBlockBlob blob = new CloudBlockBlob(new Uri(sasUri));          
            
            try
            {
                content = await blob.DownloadTextAsync();

                Console.WriteLine("Read operation succeeded for SAS {0}", sasUri);
                Console.WriteLine();
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 403)
                {
                    Console.WriteLine("Read operation failed for SAS {0}", sasUri);
                    Console.WriteLine("Additional error information: " + e.Message);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
                }
            }

            return content;

        }

        /// <summary>
        /// Internal callback method that fired when twin json is changed on server side
        /// </summary>        
        private async Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            try
            {              
                Console.WriteLine("Desired property change:");
                Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));

                //Checking that changed properties is related to blob extended properties
                if (desiredProperties[blobConfigPropertyName] != null)
                {
                    var content = await DownloadBlobAsync(desiredProperties[blobConfigPropertyName]);

                    BlobPropertyUpdatedArgs args = new BlobPropertyUpdatedArgs();
                    args.BlobContent = content;
                    args.DateTimeUpdated = desiredProperties[blobConfigPropertyName]["ts"]; //add checking dates

                    //Rais event
                    OnBlobPropertyUpdatedEvent(args, this);

                    //Notify IoT hub twin json file that we successfully download the extended blob file
                    await NotifyIoTHubOfUpdatedBlob(true);
                }
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error when receiving desired property: {0}", exception);
                }
            }
            catch (Exception ex)
            {
                await NotifyIoTHubOfUpdatedBlob(false);

                Console.WriteLine();
                Console.WriteLine("Error when receiving desired property: {0}", ex.Message);
            }          
        }
        /// <summary>
        /// Internal method that notify IoT hub that we update twin
        /// </summary>
        /// <param name="Success">Bool variable that will notify as successful or failed extended sync process</param>  
        private async Task NotifyIoTHubOfUpdatedBlob(bool Success) {

            try
            {
                if (Success)
                {
                    await client.UpdateReportedPropertiesAsync(new TwinCollection("{'configurationBlob': {'status': 'recieved'}}"));
                }
                else
                {
                    await client.UpdateReportedPropertiesAsync(new TwinCollection("{'configurationBlob': {'status': 'failed'}}"));
                }
            }
            catch (Exception ex) {
                Console.WriteLine();
                Console.WriteLine("Error when reporting reported property: {0}", ex.Message);
            }

            Console.WriteLine();
            Console.WriteLine("Reported properties updated successfully with value:", Success);
        }

        public class BlobPropertyUpdatedArgs : EventArgs
        {
            public string BlobContent { get; set; }
            public DateTime DateTimeUpdated { get; set; }
        }
    }
}
