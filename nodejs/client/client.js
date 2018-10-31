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

const applyBlob = async function(desired, twin) {
    
    const isUriEqual = (currentBlobUri === desired.uri);
    const isTsEqual = (currentBlobTs === desired.ts);

    if (isUriEqual && isTsEqual) {
        console.log(`Already using the desired uri and timestamp for ${blobPropertyName}`);
        return;
    }

    if (!desired.uri) {
        console.log(`Unable to apply empty uri for ${blobPropertyName}`);
        return;
    }

    const response = await request.get(desired.uri);

    // Process the blob depending on the content type and the more specific needs of 
    // the solution. The end to end demo in this sample code's documentation is using
    // text files with a variety of ASCII art, which we are printing to the console.
    console.log(response);

    // Here we are tracking, in-memory, the uri and ts of the applied blob. 
    currentBlobUri = desired.uri;
    currentBlobTs = desired.ts;

    const reportedPatch = {};
    reportedPatch[blobPropertyName] = {
        uri: currentBlobUri,
        ts: currentBlobTs
    };

    twin.properties.reported.update(reportedPatch, (err) => {
        if (err) {
            console.error(err);
        }
    })
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

                let desired = twin.properties.desired[blobPropertyName]; 
                if (desired) {
                    await applyBlob(desired, twin);
                }

                twin.on(`properties.desired.${blobPropertyName}`, (desired) => {
                    applyBlob(desired, twin);
                });
            })();
        });

    }
});