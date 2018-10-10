---
services: iot-hub 
platforms: nodejs,python,dotnet
author: druttka
---

# How To Extend Azure IoT Hub Twins via Azure Blob Storage  

This sample demonstrates how to extend Azure IoT Hub Twins via Azure Blob Storage. Concretely, a solution can benefit from this approach if the twin properties either [exceed the current limit](https://feedback.azure.com/forums/907045-azure-iot-edge/suggestions/33583492-iot-hub-device-and-module-twins-limit) or refer to binary content that cannot be easily represented in the twin's JSON payload.

## Getting Started

### Prerequisites

- An Azure IoT Hub device which receives twin updates from Azure IoT Hub. You can refer to the [Get started with device twins (Node)](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-node-node-twin-getstarted) example to implement this.
- An Azure Storage account with a blob container. You can refer to the [Quickstart](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-nodejs) to accomplish this.
- See the README.md in each platform directory for any platform specific prerequisites.

### Quickstart
- Retrieve the device connection string for your IoT Hub device; e.g., execute the following Azure CLI command `az iot hub device-identity show-connection-string --hub-name YourIoTHubName --device-id MyNodeDevice --output table`
- See the README.md in each platform directory for further platform specific instructions
