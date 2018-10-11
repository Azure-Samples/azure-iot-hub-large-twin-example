# IoT device twin bigger than 8kb  

This is a quickstart sample to illustrate how to manage and deploy configurations bigger than 8kb on to the device twins. We will be using blob storage to host the file needed and our python program will be able to understand these new elements and download the needed files.

## Getting Started

This sample assumes that you have familiarity with the following blog post: [Get started with device twins (Python)](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-python-twin-getstarted). The requirements to deploy this solution are the following:

- Azure CLI (>= 2.0.46) for following the Bash commands.
- NodeJS if you would be using the IoT Hub explorer (Mac or Linux)
- [Azure IoT CLI extension](https://github.com/Azure/azure-iot-cli-extension#step-1-install-the-extension)


## Step by Step

These steps will take you from creating an IoT Hub all the way to deploying a simulated device and verifying that the blob information is updated.

If you plan on following the Azure CLI commands, you can define the following environment variables that will make the commands very easy to use:

```bash
export RESOURCE_GROUP="some-rg-01"
export LOCATION="westus"
export IOT_HUB_NAME="myCoolSuperIotHub"
export DEVICE_PREFIX_NAME="mycooldevice"
```

### Create an IoT Hub 

You can use the [portal](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-python-twin-getstarted#create-an-iot-hub) or run the following commands with your Azure CLI:

```bash
az iot hub create -g $RESOURCE_GROUP -n $IOT_HUB_NAME --sku S1 -l $LOCATION
```

Obtain the credentials to the IoT hub:

```bash
export IOT_CON_STRING=`az iot hub show-connection-string -n $IOT_HUB_NAME -g $RESOURCE_GROUP -o tsv`
```


### Create a new device in IoT Hub

Let's create a random sufix for our devices that will be used in the creation of all the devices:

```bash
export RAND_SUFIX=$(cat /dev/urandom | env LC_CTYPE=C tr -dc 'a-z0-9' | fold -w 4 | head -n 1)
```

Create a random number as an identifier for our sensor:

```bash
export SENSOR_ID="${DEVICE_PREFIX_NAME}-${RAND_SUFIX}"
```

Create a new entry in IoT hub:

```bash
az iot hub device-identity create -d $SENSOR_ID --hub-name $IOT_HUB_NAME -g $RESOURCE_GROUP
```

This connection string will be used later to associate the actual simulated device to the hub. Let's save this connection for later:

```bash
export SENSOR_CS=$(az iot hub device-identity show-connection-string -d $SENSOR_ID --hub-name $IOT_HUB_NAME -g $RESOURCE_GROUP -o tsv)
```

### Upload the payload to blob storage

Create an azure storage account:

```bash
az storage account create \
    --name $AZURE_STORAGE_ACCOUNT \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku Standard_LRS \
    --encryption blob
```

Obtain the credentials and store them in a variable:

```bash
export AZURE_STORAGE_CONNECTION_STRING=`az storage account show-connection-string -g $RESOURCE_GROUP -n $AZURE_STORAGE_ACCOUNT -o tsv`
```

Create a container to store the blob:

```bash
az storage container create --name $AZURE_STORAGE_CONTAINER \
    --account-name $AZURE_STORAGE_ACCOUNT \
    --connection-string $AZURE_STORAGE_CONNECTION_STRING
```

Upload the sample file, make sure you are in the root directory of this repository:

```bash
az storage blob upload \
    --container-name $AZURE_STORAGE_CONTAINER \
    --account-name $AZURE_STORAGE_ACCOUNT \
    --connection-string $AZURE_STORAGE_CONNECTION_STRING \
    --name payload.txt \
    --file sample-files/payload.txt
```

You can also use the [Azure Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/) and the Azure Storage Connection String, that you can obtain running the following command:

```bash
echo $AZURE_STORAGE_CONNECTION_STRING
```

### Add the blob storage reference to the device twin

Download the json file of the device twin:

```bash
az iot hub device-twin show -n $IOT_HUB_NAME -g $RESOURCE_GROUP  -d $SENSOR_ID > $SENSOR_ID-device-twin.json
```

Edit the file with VS Code:

```bash
code $SENSOR_ID-device-twin.json
```

In this file, we are going to add the properties of the blob file to be utilized. We will need the URL for the payload in the blob storage which can be obtained in the portal or running the following command:

```bash
echo "https://${AZURE_STORAGE_ACCOUNT}.blob.core.windows.net/${AZURE_STORAGE_CONTAINER}/payload.txt"
```


With this information go to the json file and inside of `properties` > `desired`, right before `metadata` add the following chunk:

```json
"configurationBlob": {
    "uri": "< Full URL to your payload text >",
    "ts": "2018-10-09T19:40:18.7138092Z",
    "contentType": "txt"
},
```

After editing your file, it should look something like this:

```bash
{
  "authenticationType": "sas",
  "capabilities": {
    "iotEdge": false
  },
  "cloudToDeviceMessageCount": 0,
  "connectionState": "Connected",
  "deviceEtag": "NjAxOD10MDkw",
  "deviceId": "mycooldevice-ksn6",
  "etag": "AAAAAAAAAE=",
  "lastActivityTime": "2018-10-10T05:19:03.6950741+00:00",
  "properties": {
    "desired": {
      "configurationBlob": {
        "uri": "https://iotdevtwins101.blob.core.windows.net/devstwinscontainer/payload.txt",
        "ts": "2018-10-09T19:40:18.7138092Z",
        "contentType": "txt"
      },
      "$metadata": {
        "$lastUpdated": "2018-10-09T22:17:26.4589674Z"
      },
      "$version": 1
    },
    "reported": {
      "$metadata": {
        "$lastUpdated": "2018-10-09T22:17:26.4589674Z"
      },
      "$version": 1
    }
  },
  "status": "enabled",
  "statusUpdateTime": "0001-01-01T00:00:00+00:00",
  "version": 2,
  "x509Thumbprint": {
    "primaryThumbprint": null,
    "secondaryThumbprint": null
  }
}

```

Save the file and update the file:

```bash
az iot hub device-twin replace  -n $IOT_HUB_NAME \
    -g $RESOURCE_GROUP -d $SENSOR_ID \
    -j $SENSOR_ID-device-twin.json
```

We can confirm the changes to the device twin by getting the latest copy of the file:

```bash
az iot hub device-twin show -n $IOT_HUB_NAME -g $RESOURCE_GROUP  -d $SENSOR_ID
```

### Get ready to receive some data 

If you are using Mac or Linux let's use the [Azure IoT CLI extension](https://github.com/Azure/azure-iot-cli-extension#step-1-install-the-extension). If you are using windows you can also use the [IoT Hub Explorer Desktop Application](https://github.com/Azure/azure-iot-sdk-csharp/tree/master/tools/DeviceExplorer). This command will stream all the information that your device is inserting, you can run this command in a new terminal and leave it open to monitor the communication of your device:

Let's connect to the hub:

```bash
az iot hub monitor-events --login $IOT_CON_STRING -d $SENSOR_ID
```

After we have our environment up and running is time to create our clients. We will create a simulated IoT sensor that will provide the information captured by our device and push it into the IoT Hub


### Emulate a device using a Docker container in your computer

 Thanks to docker we can emulate an IoT device in your computer and see how it behaves. We will be using a dockerfile with all the needed python libraries to run our code. You can see the code in the `/device-twin-client` folder. 

 Build the image and tag it:

```bash
cd device-twin-client
docker build -t $USER/device-twin-client .
```

Run it:

```bash
docker run -e "CONNECTION_STRING=${SENSOR_CS}" -e "DEVICE_ID=${SENSOR_ID}" -t $USER/device-twin-client .
```

Now, switch back to your hub explorer to see the flowing data.

### Update the Device Twin json 

The steps to follow to accomplish this manually, would be the following:

1. Upload the file to blob storage.
2. Generate the SAS token to download the file from blob.
3. Download the json file of the device Twin
4. Replace the Blob URI on the file.
5. Upload the updated json file to the device twin and profit.

Or you can use our Azure CLI script to do it for you. Follow these steps:

1. You would need a new console with all the right environment variables already in place and that your terminal is in the main directory of this project

    ```bash
    cd azure-iot-hub-large-twin-example #Make sure you are in the root folder of this project
    ```
  
    Make sure to have all the following variables in your terminal, you can use a `.env` file to keep track of all of them and edit them with VS Code :

    ```bash
    mv python/automate-setup/.env.sample python/automate-setup/.env #Copy the file
    code python/automate-setup/.env
    ```

    The file would look like this, make sure these are all up to date:

    ```bash
    export RESOURCE_GROUP="dev-twins-rg"
    export LOCATION="westus"
    export IOT_HUB_NAME="devtwinsiot"
    export DEVICE_PREFIX_NAME="twins"
    export AZURE_STORAGE_ACCOUNT="azatwinsdemo01"
    export AZURE_STORAGE_CONTAINER="devtwinscontainer"
    export SENSOR_ID="supercooldevice0001"
    ```

    After you have all the right variables. You need to source the file:

    ```bash
    source python/automate-setup/.env
    ```

1. Give execution permissions to the script:

    ```bash
    chmod +x python/automate-setup/new-blob.sh
    ```

1. Run the script, as many times as you want. It will always create a new file:

    ```bash
    ./python/automate-setup/new-blob.sh $RESOURCE_GROUP \
    $AZURE_STORAGE_ACCOUNT \
    $AZURE_STORAGE_CONTAINER \
    $SENSOR_ID \
    python/sample-files/iotLogo.txt
    ```

1. In the meantime, go back to the other terminal running the docker instance and you will see the magic happen. Re-run the command replacing the last line with other `sample-files`:
    - `python/sample-files/iotLogo.txt`
    - `python/sample-files/payload.txt`
    - `python/sample-files/otherPayload.txt`
