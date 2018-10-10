# How To Extend Azure IoT Hub Twins via Azure Blob Storage (Node.js)

Please refer to the [general overview](../README.md) for an introduction to this sample. This `nodejs` directory contains the Node.js specific details and example code.  

## Getting Started

### Prerequisites

- An Azure subscription. You can get a [free trial here](https://azure.microsoft.com/en-us/free/).
- An Azure IoT Hub device which receives twin updates from Azure IoT Hub. You can refer to the [Get started with device twins (Node)](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-node-node-twin-getstarted) example to accomplish this.
- An Azure Storage account with a blob container. You can refer to the [Quickstart]  (https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-nodejs) to accomplish this.
- Node.js v8.11.2 or higher  
- (Optional) [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)  

### Quickstart

- Retrieve the device connection string for your IoT Hub device; e.g., execute the following Azure CLI command `az iot hub device-identity show-connection-string --hub-name YourIoTHubName --device-id MyNodeDevice --output table`
- Execute the sample client with this device connection string as an envrionment variable to the process; e.g., `DEVICE_CONNECTION_STRING="yourConnectionString" node client.js`

At this point, your client device is connected to IoT Hub and is registered to receive updates for its associated [device twin](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-device-twins).

## Demo

With the device client running, as accomplished in the Quickstart above, we would now like to update the twin with information that exceeds the size limit imposed by IoT Hub. To accomplish this, we will update the twin with a desired property which contains a reference to an external blob asset.

### Create the blob

### Update the twin

### Observe the result

