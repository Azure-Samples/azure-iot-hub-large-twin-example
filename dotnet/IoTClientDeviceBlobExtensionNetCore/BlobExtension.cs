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
    /// </summary>
    class BlobExtension
    {
        private DeviceClient client = null;

        private string lastContent = null;
        private DateTime lastTimestamp = DateTime.MinValue;
        private string lastUri = string.Empty;

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

        async public Task GetInitialTwin()
        {
            if (!string.IsNullOrWhiteSpace(lastContent))
            {
                OnBlobPropertyUpdatedEvent(new BlobPropertyUpdatedArgs
                {
                    BlobContent = lastContent,
                    DateTimeUpdated = lastTimestamp
                }, this);

                return;
            }

            var twin = await client.GetTwinAsync();
            Console.WriteLine("Twin: {0}", twin.ToJson());
            await OnDesiredPropertiesUpdate(twin.Properties.Desired, this);
        }

        /// <summary>
        /// Downloading extended blob based on link information from twin json file
        /// </summary>
        /// <param name="sasUri">The SAS URI to the blob</param>     
        async private Task<string> DownloadBlobAsync(string sasUri) {
            string content = String.Empty;
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
                    var ts = desiredProperties[blobConfigPropertyName]["ts"].Value;
                    string sasUri = desiredProperties[blobConfigPropertyName]["uri"].Value;

                    if (string.IsNullOrWhiteSpace(sasUri))
                    {
                        Console.WriteLine("Unable to apply blob update: uri is null or whitespace.");
                        return;
                    }
                    
                    if (ts <= lastTimestamp && string.Compare(sasUri, lastUri, true) == 0)
                    {
                        Console.WriteLine("Skipping blob update: for the same sas uri, the ts is less than or equal to the currently applied configuration.");
                        return;
                    }

                    lastContent = await DownloadBlobAsync(sasUri);
                    lastTimestamp = ts;
                    lastUri = sasUri;

                    BlobPropertyUpdatedArgs args = new BlobPropertyUpdatedArgs();
                    args.BlobContent = lastContent;
                    args.DateTimeUpdated = ts;

                    //Raise event
                    OnBlobPropertyUpdatedEvent(args, this);

                    //Notify IoT hub twin json file that we successfully download the extended blob file
                    await NotifyIoTHubOfUpdatedBlob(sasUri, ts.ToString("o"));
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
                Console.WriteLine();
                Console.WriteLine("Error when receiving desired property: {0}", ex.Message);
            }          
        }
        /// <summary>
        /// Internal method that notify IoT hub that we update twin
        /// </summary>
        /// <param name="sasUri">The uri to the applied blob</param>  
        /// <param name="ts">The ts of the applied blob</param>  
        private async Task NotifyIoTHubOfUpdatedBlob(string sasUri, string ts) {

            try
            {
                var json = string.Format("{{'configurationBlob': {{'uri': '{0}', 'ts': '{1}'}}}}", sasUri, ts);
                await client.UpdateReportedPropertiesAsync(new TwinCollection(json));
            }
            catch (Exception ex) {
                Console.WriteLine();
                Console.WriteLine("Error when reporting reported property: {0}", ex.Message);
            }

            Console.WriteLine();
            Console.WriteLine("Reported properties updated successfully with value:", sasUri, ts);
        }

        public class BlobPropertyUpdatedArgs : EventArgs
        {
            public string BlobContent { get; set; }
            public DateTime DateTimeUpdated { get; set; }
        }
    }
}
