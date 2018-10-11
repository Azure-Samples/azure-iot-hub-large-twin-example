'use strict';

// This example demonstrates the process of extending the twin updates to upload and 
// refer to external assets in blob storage, while the twin supplies the link to the blob

const Iothub = require('azure-iothub');
const Storage = require('azure-storage');

require('dotenv').load();

const ensureContainer = (blobService, containerName) => {

    return new Promise((resolve, reject) => {
        blobService.createContainerIfNotExists(containerName, (err, result) => {
            
            if (err) {
                return reject(err);
            }

            return resolve(result);
        });
    });
};

const uploadBlobToContainer = (blobService, containerName, blobName, localFileName) => {

    return new Promise((resolve, reject) => {

        blobService.createBlockBlobFromLocalFile(containerName, blobName, localFileName, (err, result, response) => {

            if (err) {
                return reject(err);
            }

            return resolve(result);

        });
    });
};

const generateSasUrl = (blobService, containerName, blobName) => {

    var startDate = new Date();
    var expiryDate = new Date(startDate);

    // TODO: Accept the expiry and start ranges as parameters
    expiryDate.setMinutes(startDate.getMinutes() + 100);
    startDate.setMinutes(startDate.getMinutes() - 100);

    var sharedAccessPolicy = {
        AccessPolicy: {
            Permissions: Storage.BlobUtilities.SharedAccessPermissions.READ,
            Start: startDate,
            Expiry: expiryDate
        }
    };

    var token = blobService.generateSharedAccessSignature(containerName, blobName, sharedAccessPolicy);
    return blobService.getUrl(containerName, blobName, token);

};

(async () => {
    
    try 
    {
        const jobClient = Iothub.JobClient.fromConnectionString(process.env.IOT_HUB_CONNECTION_STRING);
        const containerName = process.env.STORAGE_CONTAINER_NAME;

        const blobService = Storage.createBlobService(process.env.STORAGE_ACCOUNT_NAME, process.env.STORAGE_ACCOUNT_KEY);
        await ensureContainer(blobService, containerName);
    
        // TODO: accept blob and local file names from args or env
        await uploadBlobToContainer(blobService, containerName, 'payload', '../../sample-files/payload.txt');
        const sasUrl = await generateSasUrl(blobService, containerName, 'payload');
        
        // TODO: accept query from args or env; use tags or properties to apply at scale
        const condition = "tags.platform='node'";

        const patch = {
            etag: '*',
            properties: {
                desired: {
                    configurationBlob: {
                        uri: sasUrl,
                        ts: new Date().toISOString()
                    }
                }
            }
        };

        jobClient.scheduleTwinUpdate(`configurationBlob-${new Date().getTime()}`, condition, patch, new Date(), 300, (err, result) => {
            
            if (err) {
                console.error(err);
                return;
            }

            console.log('Successfully submitted job to apply twin updates:');
            console.dir(result);

        });
    }
    catch (err) {
        console.error('Failed to update twins with the desired blob:');
        console.error(err);
    }

})();