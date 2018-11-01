# How To Extend Azure IoT Hub Twins via Azure Blob Storage (Node.js)

Please refer to the [root README](../README.md) for a high level introduction of this sample. This document covers the details of the specific `python` implementation.  

## Getting Started

### Prerequisites

The [COMMON_SETUP.md](../COMMON_SETUP.md) file contains more detailed references on how to provision the required resources (e.g., IoT Hub, device identity, Storage Account, blob container).

The python example also requires
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) (>= 2.0.46)
- [Azure IoT CLI extension](https://github.com/Azure/azure-iot-cli-extension#step-1-install-the-extension)
- *Either* of the following
  - [Docker](https://www.docker.com/get-started)
  - [Python](https://www.python.org/downloads/) 2.7+ or 3.6+

### Quickstart

**Start the simulated device client**

In the `device-twin-client` directory, you can choose to run the client example as a python app on your own host or inside a Docker container. 

*In either case*

- Retrieve the device connection string for your IoT Hub device
  - One option is to execute the following [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) command `az iot hub device-identity show-connection-string --hub-name YourIoTHubName --device-id MyNodeDevice --output table`
  - Another option is to retrieve the connection string from the Azure Portal as described in the [Create a device identity](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-device-management-get-started#create-a-device-identity) documentation.

*For the Docker experience*

- Build the image and tag it: `docker build -t localhost:5000/device-twin-client .`
- Run the container, being sure to replace the values for the CONNECTION_STRING and DEVICE_ID environment variables: `docker run -e "CONNECTION_STRING=${SENSOR_CS}" -e "DEVICE_ID=${SENSOR_ID}" -t localhost:5000/device-twin-client .`

*To run the python client directly*

- Install the Azure IoT Hub device client module: `pip install azure-iothub-device-client`
- Run the application: `python ./sender/device-twin-sample.py`

At this point, your client device is connected to IoT Hub and is registered to receive updates for its associated [device twin](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-device-twins).

**Apply a new asset**

In the `automate-setup` directory,

- Copy `.env.sample` to `.env` and update the values
- Source the `.env` file: `source .env`
- Ensure the script is executable: `chmod +x new-blob.sh`
- Execute the backend script to upload and apply a new blob: 
    ```
    ./new-blob.sh $RESOURCE_GROUP \
        $AZURE_STORAGE_ACCOUNT \
        $AZURE_STORAGE_CONTAINER \
        $SENSOR_ID \
        ./sample-files/iotLogo.txt
    ```

If you left the client running, you should see the new artwork displayed in the console.

## Details

### Client 

- Opens the device client connection (`iothub_client_init`)
- Registers for device twin updates (`client.set_device_twin_callback`)
- Enters a loop to send arbitrary sample messages to IoT Hub (`client.send_event_async`)
- When device twin updates are received (`device_twin_callback`)
  - Parses the JSON twin (`json.loads(payload)`)
  - Retrieves the blob via the SAS URL (`urllib.request.urlopen(url)`)
  - Prints it to the console (`print(text)`)

### Back-end

The `automate-setup/new-blob.sh` script automates the manual process of

- Uploading a file to blob storage (`az storage blob upload`)
- Generating a SAS token (`az storage blob generate-sas`)
- Patching the twin property for a single device identity 
  - Obtains the current twin (`az iot hub device-twin show`)
  - Updates the desired blob properties (`jq...`)
  - Replaces the twin (`az iot hub device-twin replace`)