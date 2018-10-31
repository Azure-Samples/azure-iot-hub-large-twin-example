'use strict';

// This example is based upon the Getting Started with Device Twins (Node) example.
// https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-node-node-twin-getstarted
// It demonstrates the process of extending the twin updates to fetch and apply 
// external assets from blob storage, while the twin supplies the link to the blob

const Client = require('azure-iot-device').Client;
const Protocol = require('azure-iot-device-mqtt').Mqtt;
const request = require('request-promise-native');

const connectionString = process.env.DEVICE_CONNECTION_STRING;
const blobPropertyName = process.env.BLOB_PROPERTY_NAME || 'configurationBlob';

const client = Client.fromConnectionString(connectionString, Protocol);

let currentBlobUri;
let currentBlobTs;
let blobContents;

const applyBlob = async function(updatedBlob) {
    
    const isUriEqual = (currentBlobUri === updatedBlob.uri);
    const isTsEqual = (currentBlobTs === updatedBlob.ts);

    if (isUriEqual && isTsEqual) {
        console.log(`Already using the desired uri and timestamp for ${blobPropertyName}`);
        return;
    }

    if (!updatedBlob.uri) {
        console.log(`Unable to apply empty uri for ${blobPropertyName}`);
        return;
    }

    const response = await request.get(updatedBlob.uri);

    // Process the blob depending on the content type and the more specific needs of 
    // the solution. Here we are expecting it to be an additional JSON payload which 
    // contains an "items" array, and we are simply logging the length of that array.
    blobContents = JSON.parse(response);    
    console.log(`${blobContents.items.length} items`);

    // Here we are tracking, in-memory, the uri and ts of the applied blob. 
    currentBlobUri = updatedBlob.uri;
    currentBlobTs = updatedBlob.ts;

    // TODO: Report properties; see #18
}

client.open(function (err) {

    if (err) {
        
        console.error('could not open IotHub client');

    } else {
        
        console.log('client opened');

        client.getTwin(function (err, twin) {
            
            if (err) {
                console.log(err);
            }

            (async () => {

                let blobValue = twin.properties.desired[blobPropertyName]; 
                if (blobValue) {
                    await applyBlob(blobValue);
                }

                twin.on(`properties.desired.${blobPropertyName}`, function(delta) {
                    applyBlob(delta);
                });
            })();
        });

    }
});