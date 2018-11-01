# How To Extend Azure IoT Hub Twins via Azure Blob Storage (Node.js)

Please refer to the [root README](../README.md) for a high level introduction of this sample. This document covers the details of the specific `nodejs` implementation.  

## Getting Started

### Prerequisites

The [COMMON_SETUP.md](../COMMON_SETUP.md) file contains more detailed references on how to provision the required resources (e.g., IoT Hub, device identity, Storage Account, blob container).

### Quickstart

**Start the simulated device client**

In the `client` directory,

- Retrieve the device connection string for your IoT Hub device; e.g., execute the following [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) command `az iot hub device-identity show-connection-string --hub-name YourIoTHubName --device-id MyNodeDevice --output table`
- Execute the sample client with this device connection string as an envrionment variable to the process; e.g., `DEVICE_CONNECTION_STRING="yourConnectionString" node client.js`

At this point, your client device is connected to IoT Hub and is registered to receive updates for its associated [device twin](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-device-twins).

**Apply a new asset**

In the `backend` directory,

- Copy `.env.template` to `.env` and update the values
  - Note that the DEVICE_QUERY_CONDITION in the template is intentionally set to a query which returns no devices
  - Note that the IOT_HUB_CONNECTION_STRING is **not the device connection string**, but rather an iothubowner connection; see [Access Control and Permissions](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-security#iot-hub-permissions) and [Understand Different Connection Strings in Azure IoT Hub](https://blogs.msdn.microsoft.com/iotdev/2017/05/09/understand-different-connection-strings-in-azure-iot-hub/).
- Execute the backend script to upload and apply a new blob; e.g., `node server.js mynewblob ../../sample-files/payload.txt`

If you left the client running, you should see the new artwork displayed in the console.

## Details

### Client 

- Opens the device client connection (`client.open`)
- Gets the device twin (`client.getTwin`)
- Applies any existing configuration (`await applyBlob(...)`)
- Registers a callback for updates to the desired properties (`twin.on(...)`)

The `applyBlob` function acts on the updated properties.

- If it's the same uri and timestamp that we're already using, return
- If there's no uri to follow, return
- Retrieve the contents via the SAS URL (`request.get`)
- Print the contents to the console (`console.log`)
  - This example uses ASCII art text files, but other solutions might parse JSON, parse CSV files, display an image...)
- Track the current uri and timestamp in memory
- Patch the reported properties of the twin with the uri and timestamp that the device is now using (`twin.properties.reported.update(...)`)

### Back-end

This example uploads a new blob, generates a new SAS url, and submits a job to IoT Hub. The job takes on the responsibility of applying the update to each matched twin.

- Initializes an IoT Hub job client (`Iothub.JobClient.fromConnectionString`)
- Initializes a blob storage service (`Storage.createBlobService`)
- Ensures that the target blob container exists (`ensureContainer`)
- Uploads the blob (`uploadBlobToContainer`)
- Generates a SAS URL for the blob (`generateSasUrl`)
- Submits a job to update device twins for a given device query condition (`jobClient.scheduleTwinUpdate`)

As alternatives, consider

- the `dotnet` example in this repo uses an Azure Function triggered from blob updates; it iterates over the results of a device query to patch each twin
- the `python` example in this repo uses the Azure CLI to replace the twin for a single device by id