---
services: azure-iot-hub-large-twin-example
platforms: dotnet-core
author: Kohei Kawata
---

# How To Extend Azure IoT Hub Twins via Azure Blob Storage (.NET)

Please refer to the [root README](../README.md) for a high level introduction of this sample. This document covers the details of the specific `dotnet` implementation.  

## Getting Started

### Prerequisites

The [Prerequisites section of the root README](../README.md#prerequisites) contains more detailed references on how to provision the required resources:
- an Azure IoT Hub
  - with a device identity
- an Azure Storage account
  - with a blob container

This platform solution also requires
- [.NET Core 2.1 SDK](https://www.microsoft.com/net/download)

### Quickstart

**Start the simulated device client**

The `IoTClientDeviceBlobExtensionNetCore` project can be run as a console application which simulates a connected device.

- Retrieve the device connection string for your IoT Hub device
  - One option is to execute the following [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) command `az iot hub device-identity show-connection-string --hub-name YourIoTHubName --device-id MyNodeDevice --output table`
  - Another option is to retrieve the connection string from the Azure Portal as described in the [Create a device identity](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-device-management-get-started#create-a-device-identity) documentation.
- Open the `IoTClientDeviceBlobExtensionNetCore` project
  - Add the device connection string to this line in `Proram.cs`: `static string DeviceConnectionString = "Put Device Connection string here";`
  - Build and run the program

At this point, your client device is connected to IoT Hub and is registered to receive updates for its associated [device twin](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-device-twins).

**Deploy and configure the back-end solution**

The `IoTHubExtension` project is meant to be hosted as an Azure Function. One of the easiest ways to deploy this would be:

- Open the `IoTHubExtension` project in [VS Code](https://code.visualstudio.com/download)
- Install the [Azure Functions extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azurefunctions) for VS Code
- Use the Azure Functions extension to create and deploy a function 

![Azure Functions extension toolbar](../images/AzureFunctionsExtensionToolbar.png)

> TODO: Add the more complete details about configuring the blob trigger, IoT Hub connection string, and device query condition 

## Details

### Client 

- Opens the device client connection (`DeviceClient.CreateFromConnectionString`)
- Gets the device twin (`Client.GetTwinAsync`)
- Initializes a `BlobExtension`, an abstraction provided in this project to handle each twin update and raise a secondary event after the blob asset is retrieved

The `BlobExtension` acts on the updated properties.

- Subscribes to device twin updates (`client.SetDesiredPropertyUpdateCallbackAsync`)
- When the twin is updated
  - Downloads the new blob (`DownloadTextAsync`)
  - Raises its `BlobPropertyUpdated` event with details about the new blob asset
  - Patches the twin's reported properties to acknowledge the update (`client.UpdateReportedPropertiesAsync`)

### Back-end

![Sample diagram](IotHubExtendingTwin.png)

- The function is triggered by updates to blobs in the configured container (`BlobTrigger`)
- A new SAS URL is generated for the blob (`GetSharedAccessSignature`)
- A device query is performed against the IoT Hub device registry (`registryManager.CreateQuery`)
- For each returned device twin (`query.GetNextAsTwinAsync`), the twin's desired properties are updated with the new SAS URL (`twin.Properties.Desired = ...`)
- The patched twins are submitted back to the IoT Hub registry (`registryManager.UpdateTwins2Async`)