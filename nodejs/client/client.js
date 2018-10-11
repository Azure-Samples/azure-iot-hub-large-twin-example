'use strict';

// This example is based upon the Getting Started with Device Twins (Node) example.
// https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-node-node-twin-getstarted
// It demonstrates the process of extending the twin updates to fetch and apply 
// external assets from blob storage, while the twin supplies the link to the blob

const Client = require('azure-iot-device').Client;
const Mqtt = require('azure-iot-device-mqtt').Mqtt;
const request = require('request-promise-native');

let blobState = {};

const subscribeToBlobChanges = (client, propertyName, pipeline) => {

    return new Promise((resolve, reject) => {

        client.getTwin((err, twin) => {
            
            if (err) {
                return reject(err);
            }
    
            twin.on(`properties.desired.${propertyName}`, async (delta) => {

                const updatedState = {
                    delta: delta,
                    status: 'pending',
                    acknowledgedUri: delta.uri
                };

                blobState = await pipeline.reduce(async (previousPromise, next) => {
                    
                    let state;

                    try {

                        state = await previousPromise;
                        return await next(state);

                    }
                    catch (err) {

                        console.error(`Failed to execute the pipeline for the updated ${propertyName} value:`);
                        console.error(err);
                        
                        state.status = err.message || 'failed';

                        return state;
                    }

                }, Promise.resolve(updatedState));

                const patch = {};
                patch[propertyName] = {
                    uri: blobState.uri,
                    acknowledgedUri: blobState.acknowledgedUri,
                    status: blobState.status
                };

                twin.properties.reported.update(patch, (err) => {

                    console.log('Attempted to report:');
                    console.dir(patch);

                    if (err) {
                        console.error(`Failed to update the reported properties for ${propertyName}:`);
                        console.error(err);
                    }

                })
            });

            return resolve(twin);
        });

    });
};

const downloadBlobContent = async (update) => {

    const delta = update.delta;

    const hasUriChanged = blobState.acknowledgedUri !== delta.uri;
    const hasTimestampChanged = blobState.ts >= delta.ts;

    if (!hasUriChanged && !hasTimestampChanged) {  
        // Preserve the pre-existing state
        update.status = blobState.status;
        update.uri = blobState.uri;
        update.ts = blobState.ts;
        update.content = blobState.content;
    } else {
        update.status = 'pending';
        update.uri = delta.uri;
        update.ts = delta.ts;    
        update.content = await request.get(delta.uri);    
    }

    return update;
};

const parseAndLogJsonContent = (update) => {

    // Do whatever is necessary with the attached blob payload.
    // For the sake of this example, consider that it is a secondary JSON payload to be 
    // parsed, containing additional configuration data.

    const parsed = JSON.parse(update.content);
    console.log(`process content item count: ${parsed.items.length}`);

    update.status = 'success';
    return update;
}

const initializeClient = (connectionString, protocol) => {
    
    return new Promise((resolve, reject) => {
    
        const client = Client.fromConnectionString(connectionString, protocol);

        client.open((err) => {

            if (err) {
                return reject('could not open IotHub client');
            }
            
            return resolve(client);
        });

    });
};

(async () => {
    
    try 
    {
        let client = await initializeClient(process.env.DEVICE_CONNECTION_STRING, Mqtt);

        await subscribeToBlobChanges(
            client, 
            process.env.BLOB_PROPERTY_NAME || 'configurationBlob', 
            [
                downloadBlobContent, 
                parseAndLogJsonContent,
            ]);

    }
    catch (err) {
        console.error('device startup failed:');
        console.error(err);
    }

})();