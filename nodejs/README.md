# How To Extend Azure IoT Hub Twins via Azure Blob Storage (Node.js)

Please refer to the [general overview](../README.md) for an introduction to this sample. This `nodejs` directory contains the Node.js specific details and example code.  

## Getting Started

### Prerequisites

- An Azure IoT Hub device which receives twin updates from Azure IoT Hub. You can refer to the [Get started with device twins (Node)](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-node-node-twin-getstarted) example to implement this.
- An Azure Storage account with a blob container. You can refer to the [Quickstart](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-nodejs) to accomplish this.
- Node.js v8.11.2 or higher

### Quickstart

- Retrieve the device connection string for your IoT Hub device; e.g., execute the following Azure CLI command `az iot hub device-identity show-connection-string --hub-name YourIoTHubName --device-id MyNodeDevice --output table`
- Execute the sample client with this device connection string as an envrionment variable to the process; e.g., `DEVICE_CONNECTION_STRING="yourConnectionString" node client.js`

