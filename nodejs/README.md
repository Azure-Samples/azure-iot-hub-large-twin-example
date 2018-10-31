# How To Extend Azure IoT Hub Twins via Azure Blob Storage (Node.js)

Please refer to the [root README](../README.md) for a high level introduction of this sample. This document covers the details of the specific `nodejs` implementation.  

## Getting Started

### Prerequisites

The [Prerequisites section of the root README](../README.md#prerequisites) contains more detailed references on how to provision the required resources:
- an Azure IoT Hub
  - with a device identity
- an Azure Storage account
  - with a blob container

### Quickstart

**Start the simulated device client**

In the `client` directory,

- Retrieve the device connection string for your IoT Hub device; e.g., execute the following Azure CLI command `az iot hub device-identity show-connection-string --hub-name YourIoTHubName --device-id MyNodeDevice --output table`
- Execute the sample client with this device connection string as an envrionment variable to the process; e.g., `DEVICE_CONNECTION_STRING="yourConnectionString" node client.js`

At this point, your client device is connected to IoT Hub and is registered to receive updates for its associated [device twin](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-device-twins).

**Apply a new asset**

In the `backend` directory,

- Copy `.env.template` to `.env` and update the values
  - Note that the DEVICE_QUERY_CONDITION in the template is intentionally set to a query which returns no devices
  - Note that the IOT_HUB_CONNECTION_STRING is **not the device connection string**, but rather an iothubowner connection; see [Access Control and Permissions](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-security#iot-hub-permissions) and [Understand Different Connection Strings in Azure IoT Hub](https://blogs.msdn.microsoft.com/iotdev/2017/05/09/understand-different-connection-strings-in-azure-iot-hub/).
- Execute the backend script to upload and apply a new blob; e.g., `node server.js mynewblob ../../sample-files/payload.txt`