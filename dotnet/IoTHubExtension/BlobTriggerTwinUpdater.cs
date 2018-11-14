using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Devices;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Blob;

namespace IoTHubExtension
{
    public static class BlobTriggerTwinUpdater
    {
        static RegistryManager registryManager;
        static string iotHubQuery;

        [FunctionName("BlobTriggerTwinUpdater")]
        public static void Run([BlobTrigger("extensions/{Uri}", Connection = "BlobStorageConnectionString")]CloudBlockBlob myBlob, string Uri, ILogger log)
        {
            EnsureInitialized();

            log.LogInformation($"C# Blob trigger function Processed blob\n Uri:{Uri} BlobUri{myBlob.StorageUri.PrimaryUri.ToString()}");
            string bloburl = GenerateSaSURI(myBlob);

            UpdateTwins(bloburl).Wait();
        }

        /// <summary>
        /// Ensures that static resources, e.g. the registry manager, have been initialized
        /// </summary>
        /// <returns></returns>
        private static void EnsureInitialized()
        {
            if (registryManager == null)
            {
                string iotHubConnectionString = Environment.GetEnvironmentVariable("iotHubConnectionString");
                registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
            }

            if (string.IsNullOrWhiteSpace(iotHubQuery))
            {
                iotHubQuery = Environment.GetEnvironmentVariable("iotHubDeviceQuery");
            }
        }

        /// <summary>
        /// Generating Blob URI with SAS token
        /// </summary>
        /// <param name="MyBlob">blob object that was update/uploaded</param>
        /// <returns></returns>
        public static string GenerateSaSURI(CloudBlockBlob MyBlob)
        {
            //Set the expiry time and permissions for the blob.
            //In this case, the start time is specified as a few minutes in the past, to mitigate clock skew.
            //The shared access signature will be valid immediately.
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5);
            sasConstraints.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(24);
            sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write;

            //Generate the shared access signature on the blob, setting the constraints directly on the signature.
            string sasBlobToken = MyBlob.GetSharedAccessSignature(sasConstraints);

            return MyBlob.Uri.ToString() + sasBlobToken;

        }
        
        /// <summary>
        /// Updating Twins of all devices that selected by input query
        /// </summary>
        /// <param name="IotHubQuery">Iot Hub devices query</param>
        /// <returns></returns>
        public static async Task UpdateTwins(string blobUrl)
        {
            var query = registryManager.CreateQuery(iotHubQuery, 100);
            List<Microsoft.Azure.Devices.Shared.Twin> twins = new List<Microsoft.Azure.Devices.Shared.Twin>();
            List<Microsoft.Azure.Devices.Shared.Twin> batchtwins = new List<Microsoft.Azure.Devices.Shared.Twin>();

            string changeDateTime = System.DateTime.UtcNow.ToString("R");
            string patch = String.Format(@"{{'configurationBlob': {{
                'uri': '{0}',
                'ts': '{1}',
                'contentType': 'json'
            }}}}", blobUrl, changeDateTime);
            
            TwinCollection collection = new TwinCollection();
            
            while (query.HasMoreResults) {

                batchtwins = (await query.GetNextAsTwinAsync()).ToList();             

                foreach (var twin in batchtwins) {

                    twin.Properties.Desired = new TwinCollection(patch);
                    twins.Add(twin);
                }
            }
            
            await registryManager.UpdateTwins2Async(twins);

        }
    }
}
