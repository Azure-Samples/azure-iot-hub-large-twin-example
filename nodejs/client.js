'use strict';

// This example is based upon the Getting Started with Device Twins (Node) example.
// https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-node-node-twin-getstarted
// It demonstrates the process of extending the twin updates to fetch and apply 
// external assets from blob storage, while the twin supplies the link to the blob

const Client = require('azure-iot-device').Client;
const Protocol = require('azure-iot-device-mqtt').Mqtt;
const request = require('request-promise-native');

const connectionString = process.env.DEVICE_CONNECTION_STRING;
const client = Client.fromConnectionString(connectionString, Protocol);

let blobReference;
let blobContents;

const applyBlob = async function(updatedBlob) {
    
    if (blobReference === updatedBlob.ts) {
        // Nothing to change
        console.log('identical ts, nothing to change');
        return;
    }

    if (!updatedBlob.uri) {
        // TODO: report failure
        console.log('no uri, nothing to do');
        return;
    }

    const response = await request.get(updatedBlob.uri);

    blobContents = JSON.parse(response);
    console.log(`${blobContents.items.length} items`);

    blobReference = updatedBlob.ts;
    console.log(`${blobReference} blob timestamps`);
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
                if (twin.properties.desired.configurationBlob) {
                    await applyBlob(twin.properties.desired.configurationBlob);
                }

                twin.on('properties.desired.configurationBlob', function(delta) {
                    applyBlob(delta);
                });
            })();
        });

    }
});