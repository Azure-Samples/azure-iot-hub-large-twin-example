'use strict';

// This example is based upon the Getting Started with Device Twins (Node) example.
// https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-node-node-twin-getstarted
// It demonstrates the process of extending the twin updates to fetch and apply 
// external assets from blob storage, while the twin supplies the link to the blob

const Client = require('azure-iot-device').Client;
const Mqtt = require('azure-iot-device-mqtt').Mqtt;
const request = require('request-promise-native');

const PipelineFailure = Symbol('pipeline.failed');

let blobTimestamp;
let blobContent;

const subscribeToBlobChanges = (client, propertyName, pipeline) => {

    return new Promise((resolve, reject) => {

        client.getTwin((err, twin) => {
            
            if (err) {
                return reject(err);
            }
    
            twin.on(`properties.desired.${propertyName}`, async (delta) => {

                const initialState = {
                    delta: delta,
                    status: 'pending'
                };

                const state = await pipeline.reduce(async (previousPromise, next) => {
                    
                    try {

                        const state = await previousPromise;

                        if (state.status === PipelineFailure) {
                            return state;
                        }

                        return await next(state);
                    }
                    catch (err) {

                        console.error(`Failed to execute the pipeline for the updated ${propertyName} value:`);
                        console.error(err);
                        return PipelineFailure;
                    
                    }

                }, Promise.resolve(initialState));

            });

            return resolve(twin);
        });

    });
};

const downloadBlobContent = async (state) => {

    const delta = state.delta;

    if (blobTimestamp === delta.ts) {
        console.warn('identical timestamp, nothing to change');
    }
    else if (!delta.uri) {
        console.warn('no uri, nothing to do');
    }
    else {
        blobContent = await request.get(delta.uri);
        blobTimestamp = delta.ts;    
    }
    
    state.content = blobContent;
    return state;
};

const parseAndLogJsonContent = (state) => {

    // Do whatever is necessary with the attached blob payload.
    // For the sake of this example, consider that it is a secondary JSON payload to be 
    // parsed, containing additional configuration data.

    const parsed = JSON.parse(state.content);
    console.log(`process content item count: ${parsed.items.length}`);

    return state;
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

        // TODO: The example does not currently send back a patch of reported properties
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