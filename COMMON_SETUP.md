# Common Setup For The Extend Azure IoT Hub Twins via Azure Blob Storage Example

Before getting started with any of the platform specific examples, there is some common infrastructure to deploy into Azure. This document is a walkthrough of setting up those resources using the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) from a bash shell. 

There are many alternatives to the Bash + Azure CLI approach. To take an alternative approach, refer instead to the following:

- To create an Azure IoT Hub and at least one connected device (physical or simulated), refer to any of the "Send telemetry" 5-Minute Quickstarts under the [IoT Hub Documentation](https://docs.microsoft.com/en-us/azure/iot-hub/).
- To create an Azure Storage account, refer to any of the "Create storage account" 5-Minute Quickstarts under the [Azure Storage Documentation](https://docs.microsoft.com/en-us/azure/storage/).
- To create a blob container within the Azure Storage account, refer to the "Blob quickstarts" of the [Azure Storage Documentation](https://docs.microsoft.com/en-us/azure/storage/). 

## Prerequisites

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) (>= 2.0.46)
- [Azure IoT CLI extension](https://github.com/Azure/azure-iot-cli-extension#step-1-install-the-extension)

## Quickstart

These steps will take you from creating an IoT Hub all the way to deploying a simulated device and verifying that the blob information is updated.

If you plan on following the Azure CLI commands, you can define the following environment variables that will make the commands very easy to use:

```bash
export RESOURCE_GROUP="some-rg-01"
export LOCATION="westus"
export IOT_HUB_NAME="myCoolSuperIotHub"
export DEVICE_PREFIX_NAME="mycooldevice"
export AZURE_STORAGE_ACCOUNT="mystorage"
export AZURE_STORAGE_CONTAINER="blobs"
```

### Login to Azure 

Before using the Azure CLI, ensure that you are logged in

```bash
az login
```

Also ensure that you have selected the desired target subscription

To list available subscriptions:

```bash
az account list -o table
```

To set the active subscription:

```bash
az account set -s {desired_subscription_id}
```

To confirm that the subscription has been set:

```bash
az account show -o table
```

### Create the resource group

If the target resource group was not previously created, create it now:

```bash
az group create -n $RESOURCE_GROUP -l $LOCATION
```

### Create an IoT Hub 

You can use the [portal](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-python-twin-getstarted#create-an-iot-hub) or run the following commands with your Azure CLI:

```bash
az iot hub create -g $RESOURCE_GROUP -n $IOT_HUB_NAME --sku S1 -l $LOCATION
```

Obtain the credentials to the IoT hub:

```bash
export IOT_CON_STRING=`az iot hub connection-string show -n $IOT_HUB_NAME -g $RESOURCE_GROUP -o tsv`
```

### Create a new device in IoT Hub

Let's create a random suffix for our devices that will be used in the creation of all the devices:

```bash
export RAND_SUFFIX=$(cat /dev/urandom | env LC_CTYPE=C tr -dc 'a-z0-9' | fold -w 4 | head -n 1)
```

Create a random number as an identifier for our sensor:

```bash
export SENSOR_ID="${DEVICE_PREFIX_NAME}-${RAND_SUFFIX}"
```

Create a new entry in IoT Hub:

```bash
az iot hub device-identity create -d $SENSOR_ID --hub-name $IOT_HUB_NAME -g $RESOURCE_GROUP
```

This connection string will be used later to associate the actual simulated device to the hub. Let's save this connection for later:

```bash
export SENSOR_CS=$(az iot hub device-identity connection-string show -d $SENSOR_ID --hub-name $IOT_HUB_NAME -g $RESOURCE_GROUP -o tsv)
```

### Upload the payload to blob storage

Create an azure storage account:

```bash
az storage account create \
    --name $AZURE_STORAGE_ACCOUNT \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku Standard_LRS \
    --encryption-services blob
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
    --file ./sample-files/payload.txt
```

You can also use the [Azure Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/) and the Azure Storage Connection String, that you can obtain running the following command:

```bash
echo $AZURE_STORAGE_CONNECTION_STRING
```
